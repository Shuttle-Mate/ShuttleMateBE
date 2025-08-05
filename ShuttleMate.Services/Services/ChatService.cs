using AutoMapper;
using Google.Apis.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ChatModelView;
using ShuttleMate.Services.Services.Infrastructure;
using System.Net.Http.Headers;
using System.Text;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net;
namespace ShuttleMate.Services.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private string _modelUrl;
        private readonly ILogger<ChatService> _logger;
        private readonly AsyncPolicy<ChatResponse> _retryPolicy;
        private readonly AsyncPolicy<ChatResponse> _circuitBreaker;
        private readonly IAsyncPolicy<ChatResponse> _policyWrap;
        public ChatService(
                IUnitOfWork unitOfWork,
                IMapper mapper,
                IConfiguration configuration,
                IHttpContextAccessor contextAccessor,
                IEmailService emailService,
                HttpClient httpClient,
                ILogger<ChatService> logger)
                {
                    _unitOfWork = unitOfWork;
                    _mapper = mapper;
                    _configuration = configuration;
                    _contextAccessor = contextAccessor;
                    _emailService = emailService;
                    _httpClient = httpClient;
                    _logger = logger;

                    _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    _apiKey = configuration["Gemini:ApiKey"]!;

            // Cấu hình Retry Policy
            _retryPolicy = Policy<ChatResponse>
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5)
                },
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} after {delay.TotalSeconds}s. Error: {outcome.Exception?.Message}");
                });

            // Cấu hình Circuit Breaker với cách tiếp cận khác
            _circuitBreaker = Policy<ChatResponse>
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5, // Ngưỡng lỗi 50%
                    samplingDuration: TimeSpan.FromSeconds(30),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromMinutes(5),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError($"Circuit broken! Will retry after {breakDelay.TotalMinutes} minutes. Reason: {ex.Result.Response}");
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit reset!");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit half-open: Testing connection...");
                    });

            // Kết hợp các policy
            _policyWrap = Policy.WrapAsync(_retryPolicy, _circuitBreaker);
        }

        public async Task<List<ChatHistoryResponse>> GetAndCleanChatHistory(Guid userId)
        {
            // Lấy múi giờ Việt Nam
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var cutoffTime = vietnamNow.AddHours(-24);

            // 1. Lấy tin nhắn mới nhất trước
            var latestMessages = await _unitOfWork.GetRepository<ChatBotLog>().Entities
                .Where(x => x.UserId == userId)
                .OrderByDescending(m => m.CreatedTime)
                .Take(20)//lấy đúng 20 tin nhắn 
                .ToListAsync();


            // 2. Xóa tin nhắn cũ hơn 24h so với tin nhắn mới nhất
            if (latestMessages.Any())
            {
                var newestMessageTime = latestMessages.Max(x => x.CreatedTime);
                var oldMessagesCutoff = newestMessageTime.AddHours(-24);

                var oldMessages = await _unitOfWork.GetRepository<ChatBotLog>().Entities
                    .Where(x => x.UserId == userId && x.CreatedTime < oldMessagesCutoff
                    ).ToListAsync();

                if (oldMessages.Any())
                {
                    await _unitOfWork.GetRepository<ChatBotLog>().DeleteAsync(oldMessages);
                    await _unitOfWork.SaveAsync();
                }
            }

            // 3. Ánh xạ kết quả trả về
            return latestMessages.Select(m => new ChatHistoryResponse
            {
                Id = m.Id,
                Role = m.Role.ToString().ToUpper(),
                Content = m.Content,
                ModelUsed = m.ModelUsed,
                CreatedTime = m.CreatedTime
            }).ToList();
        }
        public async Task<ChatResponse> SendMessage(ChatRequest request)
        {
            try
            {
                return await _policyWrap.ExecuteAsync(async () =>
                {
                    // Lấy userId từ người dùng hiện tại
                    string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
                    Guid.TryParse(userId, out Guid cb);

                    var conversationHistory = await GetConversationHistory(cb);

                    // Thêm tin nhắn mới
                    conversationHistory.Add(new ChatMessage
                    {
                        Role = "user",
                        Parts = new List<ChatPart> { new ChatPart { Text = request.Message } }
                    });

                    var requestData = new
                    {
                        system_instruction = new { parts = new[] { new { text = SystemInstruction } } },
                        contents = conversationHistory.Select(c => new
                        {
                            role = c.Role,
                            parts = c.Parts.Select(p => new { text = p.Text })
                        })
                    };

                    var jsonContent = JsonConvert.SerializeObject(requestData);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Gọi API và nhận HttpResponseMessage
                    var httpResponse = await _httpClient.PostAsync(
                        $"v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}",
                        httpContent);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        throw new ErrorException((int)httpResponse.StatusCode,
                            httpResponse.StatusCode.ToString(),
                            $"Gemini API error: {errorContent}");
                    }

                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<GeminiResponse>(responseContent);

                    if (responseData?.Candidates == null || responseData.Candidates.Count == 0)
                    {
                        throw new ErrorException(StatusCodes.Status500InternalServerError,
                            ResponseCodeConstants.INTERNAL_SERVER_ERROR,
                            "Empty response from Gemini API");
                    }

                    var aiResponse = responseData.Candidates[0].Content.Parts[0].Text;

                    // Lưu lịch sử hội thoại
                    conversationHistory.Add(new ChatMessage
                    {
                        Role = "model",
                        Parts = new List<ChatPart> { new ChatPart { Text = aiResponse } }
                    });

                    await SaveConversationHistory(conversationHistory);

                    return new ChatResponse { Response = aiResponse };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SendMessage");
                return new ChatResponse
                {
                    Response = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau."
                };
            }
        }

        private async Task<ChatResponse> HandleFallbackResponse()
        {
            return new ChatResponse
            {
                Response = "Hệ thống đang quá tải. Vui lòng thử lại sau."
            };
        }

        // Implement these methods based on your storage solution (database, cache, etc.)
        private async Task<List<ChatMessage>> GetConversationHistory(Guid userId)
        {
            // Retrieve conversation history from your storage
            // Return empty list if no history exists
            return new List<ChatMessage>();
        }

        private async Task SaveConversationHistory(List<ChatMessage> conversation)
        {
            // Lấy userId từ người dùng hiện tại (cần inject ICurrentUserService)
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);
            //Lấy giờ cả VN hiện tại
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            // Lấy 2 tin nhắn cuối cùng trong conversation để lưu 1 là user 2 là model
            var lastTwoMessages = conversation.TakeLast(2).ToList();
            foreach (var message in lastTwoMessages)
            {
                if (message == null) continue;

                // Xác định role từ tin nhắn
                var role = message.Role == "user" ? ChatBotRoleEnum.USER : ChatBotRoleEnum.MODEL;

                // Tạo mới ChatBotLog cho mỗi tin nhắn
                var chatLog = new ChatBotLog
                {
                    Role = role,
                    Content = message.Parts.FirstOrDefault()?.Text ?? string.Empty,
                    ModelUsed = "gemini-1.5-flash",
                    UserId = cb,
                    CreatedTime = vietnamNow,
                    LastUpdatedTime = vietnamNow
                };

                // Lưu vào database
                await _unitOfWork.GetRepository<ChatBotLog>().InsertAsync(chatLog);
            }
            await _unitOfWork.SaveAsync();
        }
        private readonly string SystemInstruction = @"
            🏥 1. Thông tin chung về phòng khám
            
                Tên phòng khám: Phòng khám Đa khoa ABC
            
                Địa chỉ: Số 123, đường Nguyễn Huệ, TP Quảng Ngãi
            
                Số điện thoại: 0901 234 567
            
                Email: phongkhamabc@gmail.com
            
                Giờ làm việc:
            
                    Thứ 2 - Thứ 7: 7h00 - 20h00
            
                    Chủ nhật: 7h00 - 12h00
            
            ⚕️ 2. Danh sách dịch vụ khám chữa bệnh
            STT	Tên dịch vụ	Giá tiền	Mô tả ngắn
            1	Khám tổng quát	200.000đ	Kiểm tra toàn diện sức khỏe
            2	Khám nội tổng quát	150.000đ	Chẩn đoán các bệnh lý nội khoa
            3	Siêu âm bụng tổng quát	250.000đ	Phát hiện bất thường trong ổ bụng
            4	Xét nghiệm máu cơ bản	180.000đ	Kiểm tra chỉ số máu thông thường
            5	Khám tai mũi họng	150.000đ	Kiểm tra viêm xoang, viêm họng,...
            6	Khám sản phụ khoa	250.000đ	Tư vấn, khám phụ khoa cho nữ giới
            7	Khám nhi khoa	150.000đ	Khám cho trẻ em
            👨‍⚕️ 3. Danh sách bác sĩ
            Họ và tên	Chuyên khoa	Kinh nghiệm	Lịch làm việc
            BS. Nguyễn Văn A	Nội tổng quát	15 năm	T2 - T7 (7h - 17h)
            BS. Trần Thị B	Sản phụ khoa	10 năm	T2 - CN (7h - 20h)
            BS. Lê Văn C	Tai Mũi Họng	12 năm	T2 - T7 (8h - 18h)
            BS. Phạm Thị D	Nhi khoa	8 năm	T2 - CN (7h - 20h)
            🔄 4. Chính sách đặt lịch & khám bệnh
            
                Đặt lịch: Qua điện thoại hoặc qua website (nếu có).
            
                Chính sách hủy lịch: Thông báo trước 24h.
            
                Khám không đặt lịch trước: Vẫn được phục vụ, nhưng có thể phải chờ.
            
                Thanh toán: Tiền mặt, chuyển khoản, hoặc qua ví điện tử (Momo, ZaloPay).";

    }
}

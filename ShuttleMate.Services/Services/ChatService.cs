using AutoMapper;
using Google.Apis.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ChatModelView;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

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
        private string _apiKey;
        private string _modelUrl;

        public ChatService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService, HttpClient httpClient)
        {

            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _apiKey = configuration["Gemini:ApiKey"]!;
            //_modelUrl = configuration["Gemini:ModelUrl"]!;
        }
        public async Task<ChatResponse> SendMessage(ChatRequest request)
        {

            var conversationHistory = await GetConversationHistory(request.SessionId);

            // Thêm tin nhắn mới với định dạng đúng
            conversationHistory.Add(new ChatMessage
            {
                Role = "user", // chữ thường
                Parts = new List<ChatPart> { new ChatPart { Text = request.Message } } // chữ thường
            });

            var requestData = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                    new { text = SystemInstruction }
                }
                },
                contents = conversationHistory.Select(c => new
                {
                    role = c.Role,
                    parts = c.Parts.Select(p => new { text = p.Text })
                })
            };

            var response = await _httpClient.PostAsync(
                $"v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}",
                new StringContent(JsonConvert.SerializeObject(requestData),
                Encoding.UTF8,
                "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Có lỗi xảy ra vui lòng thử lại!");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<GeminiResponse>(responseContent);

            if (responseData?.Candidates == null || responseData.Candidates.Count == 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Có lỗi xảy ra vui lòng thử lại!");
            }

            var aiResponse = responseData.Candidates[0].Content.Parts[0].Text;

            // Save the AI response to conversation history 
            conversationHistory.Add(new ChatMessage
            {
                Role = "model",
                Parts = new List<ChatPart> { new ChatPart { Text = aiResponse } }
            });

            await SaveConversationHistory(request.SessionId, conversationHistory);

            return (new ChatResponse
            {
                Response = aiResponse
            });

        }

        // Implement these methods based on your storage solution (database, cache, etc.)
        private async Task<List<ChatMessage>> GetConversationHistory(string sessionId)
        {
            // Retrieve conversation history from your storage
            // Return empty list if no history exists
            return new List<ChatMessage>();
        }

        private async Task SaveConversationHistory(string sessionId, List<ChatMessage> conversation)
        {
            // Lấy userId từ người dùng hiện tại (cần inject ICurrentUserService)
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);
            // Lấy tin nhắn cuối cùng trong conversation để lưu
            var lastMessage = conversation.LastOrDefault();
            if (lastMessage == null) return;

            // Xác định role từ tin nhắn
            var role = lastMessage.Role == "user" ? ChatBotRoleEnum.USER : ChatBotRoleEnum.MODEL;

            // Tạo mới ChatBotLog
            var chatLog = new ChatBotLog
            {
                Role = role,
                Content = lastMessage.Parts.FirstOrDefault()?.Text ?? string.Empty,
                ModelUsed = "gemini-1.5-flash", // Hoặc model bạn đang sử dụng
                UserId = cb,
                CreatedTime = DateTimeOffset.Now,
                LastUpdatedTime = DateTimeOffset.Now
            };

            // Lưu vào database thông qua UnitOfWork
            await _unitOfWork.GetRepository<ChatBotLog>().InsertAsync(chatLog);
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

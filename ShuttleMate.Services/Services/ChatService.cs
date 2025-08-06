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
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using OpenAI.Responses;
using System.Net.Http.Json;
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
        private readonly ILogger<ChatService> _logger;
        private readonly IMemoryCache _cache;

        // Policy configurations
        private readonly AsyncPolicy<ChatResponse> _retryPolicy;
        private readonly AsyncPolicy<ChatResponse> _circuitBreaker;
        private readonly IAsyncPolicy<ChatResponse> _policyWrap;

        // Rate limiting
        private static readonly ConcurrentDictionary<string, DateTime> _userLastRequestTime = new();
        private static readonly SemaphoreSlim _rateLimitLock = new(1, 1);
        private readonly int _requestsPerMinute;
        private readonly int _cacheDurationMinutes;
        private readonly int _maxDatabaseSearchAttempts = 3;


        public ChatService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IConfiguration configuration,
            IHttpContextAccessor contextAccessor,
            IEmailService emailService,
            HttpClient httpClient,
            ILogger<ChatService> logger,
            IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;

            // Load configurations
            _requestsPerMinute = _configuration.GetValue("OpenAI:RateLimiting:RequestsPerMinute", 5);
            _cacheDurationMinutes = _configuration.GetValue("Caching:DurationMinutes", 30);

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(_configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configuration["OpenAI:ApiKey"]);

            // Configure policies
            _retryPolicy = Policy<ChatResponse>
                .Handle<Exception>()
                .OrResult(r => r.Response?.Contains("rate limit") == true)
                .WaitAndRetryAsync(
                    sleepDurations: new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(3),
                        TimeSpan.FromSeconds(5)
                    },
                    onRetry: (outcome, delay, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} after {delay.TotalSeconds}s. Error: {outcome.Exception?.Message ?? outcome.Result?.Response}");
                    });

            _circuitBreaker = Policy<ChatResponse>
                .Handle<Exception>()
                .OrResult(r => r.Response?.Contains("API error") == true)
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.3,
                    samplingDuration: TimeSpan.FromSeconds(30),
                    minimumThroughput: 5,
                    durationOfBreak: TimeSpan.FromMinutes(2),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError($"Circuit broken! Will retry after {breakDelay.TotalMinutes} minutes. Reason: {ex.Exception?.Message ?? ex.Result?.Response}");
                    },
                    onReset: () => _logger.LogInformation("Circuit reset!"),
                    onHalfOpen: () => _logger.LogInformation("Circuit half-open: Testing connection..."));

            _policyWrap = Policy.WrapAsync(_retryPolicy, _circuitBreaker);
        }

        public async Task<List<ChatHistoryResponse>> GetChatHistoryByTimeWindow(int number, Guid userId)
        {
            // Lấy múi giờ Việt Nam
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            // Tính toán khoảng thời gian dựa trên number
            var endTime = vietnamNow.AddHours(-24 * number);
            var startTime = endTime.AddHours(-24);

            _logger.LogInformation($"Loading messages between {startTime} and {endTime} for user {userId}");


            // Lấy tin nhắn trong khoảng thời gian 24h tương ứng
            var messages = await _unitOfWork.GetRepository<ChatBotLog>().Entities
                .Where(x => x.UserId == userId
                         && x.CreatedTime >= startTime
                         && x.CreatedTime < endTime)
                .OrderByDescending(m => m.CreatedTime)
                //.Take(20) // Lấy tối đa 20 tin nhắn
                .ToListAsync();

            // Ánh xạ kết quả trả về
            return messages.Select(m => new ChatHistoryResponse
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
                    // Get user ID
                    string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
                    Guid.TryParse(userId, out Guid cb);

                    // Check cache first
                    var cacheKey = $"{userId}_{request.Message.GetHashCode()}";
                    if (_cache.TryGetValue(cacheKey, out ChatResponse cachedResponse))
                    {
                        _logger.LogInformation("Returning cached response");
                        return cachedResponse;
                    }

                    // Apply rate limiting
                    await _rateLimitLock.WaitAsync();
                    try
                    {
                        if (_userLastRequestTime.TryGetValue(userId, out var lastRequest))
                        {
                            var timeSinceLastRequest = DateTime.UtcNow - lastRequest;
                            if (timeSinceLastRequest.TotalSeconds < 60.0 / _requestsPerMinute)
                            {
                                return new ChatResponse
                                {
                                    Response = "Bạn đang gửi tin nhắn quá nhanh. Vui lòng chờ giây lát."
                                };
                            }
                        }
                        _userLastRequestTime[userId] = DateTime.UtcNow;
                    }
                    finally
                    {
                        _rateLimitLock.Release();
                    }

                    // Prepare conversation
                    var systemInfo = await GetSystemInformation(cb);
                    var conversationHistory = await GetConversationHistory(cb);

                    var messages = new List<object>
                    {
                        new { role = "system", content = systemInfo }
                    };

                    foreach (var message in conversationHistory)
                    {
                        messages.Add(new
                        {
                            role = message.Role == "model" ? "assistant" : message.Role,
                            content = message.Parts.FirstOrDefault()?.Text
                        });
                    }
                    messages.Add(new { role = "user", content = request.Message });

                    // Call OpenAI API
                    var requestData = new
                    {
                        model = _configuration["OpenAI:Model"] ?? "gpt-3.5-turbo",
                        messages,
                        temperature = 0.3,
                        max_tokens = _configuration.GetValue("OpenAI:MaxTokens", 100),
                        top_p = 1.0,
                        frequency_penalty = 0.5,
                        presence_penalty = 0.5
                    };

                    var response = await _httpClient.PostAsJsonAsync("chat/completions", requestData);

                    // Handle rate limits from API
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5);
                        await Task.Delay(retryAfter);
                        return await SendMessage(request); // Retry after delay
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"OpenAI API Error: {response.StatusCode} - {errorContent}");
                        throw new ErrorException((int)response.StatusCode,
                            response.StatusCode.ToString(),
                            $"OpenAI API error: {errorContent}");
                    }

                    var content = await response.Content.ReadFromJsonAsync<ModelViews.ChatModelView.OpenAIResponse>();
                    if (content?.Choices == null || content.Choices.Count == 0)
                    {
                        throw new ErrorException(StatusCodes.Status500InternalServerError,
                            ResponseCodeConstants.INTERNAL_SERVER_ERROR,
                            "Empty response from OpenAI API");
                    }

                    var aiResponse = content.Choices[0].Message.Content;

                    // Cache the response
                    _cache.Set(cacheKey, new ChatResponse { Response = aiResponse },
                        TimeSpan.FromMinutes(_cacheDurationMinutes));

                    // Save conversation history
                    await SaveConversationHistory(new List<ChatMessage>
                    {
                        new ChatMessage
                        {
                            Role = "user",
                            Parts = new List<ChatPart> { new ChatPart { Text = request.Message } }
                        },
                        new ChatMessage
                        {
                            Role = "assistant",
                            Parts = new List<ChatPart> { new ChatPart { Text = aiResponse } }
                        }
                    });

                    return new ChatResponse { Response = aiResponse };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
                return new ChatResponse
                {
                    Response = "Xin lỗi, tôi đang gặp sự cố kỹ thuật. Vui lòng thử lại sau."
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
        private async Task<string> GetSystemInformation(Guid userId)
        {
            var user = await _unitOfWork.GetRepository<User>()
                .Entities
                .Include(u => u.School)
                .ThenInclude(s => s.SchoolShifts)
                .Include(u => u.School)
                .ThenInclude(s => s.Routes)
                .ThenInclude(r => r.Tickets)
                .FirstOrDefaultAsync(x => x.Id == userId && !x.DeletedTime.HasValue && x.Violate == false);

            if (user == null)
            {
                return "Không tìm thấy thông tin người dùng hoặc người dùng bị hạn chế";
            }

            var sb = new StringBuilder();
            sb.AppendLine("### THÔNG TIN HỆ THỐNG SHUTTLEMATE");
            sb.AppendLine($"Người dùng: {user.FullName} ({user.Email})");

            // Xử lý khi user có trường học
            if (user.SchoolId != null && user.School != null)
            {
                var school = user.School;
                var currentSemester = await GetCurrentSemester(new List<School> { school });

                sb.AppendLine($"\n**THÔNG TIN TRƯỜNG HỌC CỦA BẠN**");
                sb.AppendLine($"- Tên trường: {school.Name}");
                sb.AppendLine($"- Địa chỉ: {school.Address}");
                sb.AppendLine($"- Học kỳ hiện tại: {currentSemester}");

                sb.AppendLine($"\n**LỊCH HỌC**");
                foreach (var shift in school.SchoolShifts.OrderBy(s => s.Time))
                {
                    sb.AppendLine($"- Ca {shift.ShiftType}: {shift.Time} ({shift.SessionType})");
                }

                sb.AppendLine($"\n**TUYẾN XE CỦA TRƯỜNG BẠN**");
                foreach (var route in school.Routes.Where(r => r.IsActive))
                {
                    sb.AppendLine($"- Tuyến {route.RouteName} ({route.RouteCode})");
                    sb.AppendLine($"  Thời gian hoạt động: {route.OperatingTime}");

                    var tickets = route.Tickets.GroupBy(t => t.Type);
                    foreach (var ticketGroup in tickets)
                    {
                        var firstTicket = ticketGroup.First();
                        sb.AppendLine($"  + Vé {GetTicketTypeName(ticketGroup.Key)}: {firstTicket.Price.ToString("N0")} VND");
                    }
                }
            }
            else
            {
                // Xử lý khi user chưa có trường học
                var allSchools = await _unitOfWork.GetRepository<School>()
                    .Entities
                    .Include(s => s.Routes)
                    .ThenInclude(r => r.Tickets)
                    .Where(x => !x.DeletedTime.HasValue)
                    .OrderBy(x => x.Name) // Có thể thay bằng sắp xếp theo khoảng cách nếu có thông tin vị trí
                    .Take(3) // Lấy 3 trường tiêu biểu
                    .ToListAsync();

                sb.AppendLine("\n**BẠN CHƯA CÓ TRƯỜNG HỌC ĐƯỢC GÁN**");
                sb.AppendLine("Dưới đây là một số trường học tiêu biểu trong hệ thống:");

                foreach (var school in allSchools)
                {
                    sb.AppendLine($"\n- Trường: {school.Name} ({school.Address})");

                    var popularRoutes = school.Routes
                        .Where(r => r.IsActive)
                        .OrderByDescending(r => r.Tickets.Count)
                        .Take(2); // Lấy 2 tuyến phổ biến nhất

                    if (popularRoutes.Any())
                    {
                        sb.AppendLine("  Các tuyến xe phổ biến:");
                        foreach (var route in popularRoutes)
                        {
                            sb.AppendLine($"  + Tuyến {route.RouteName} ({route.RouteCode})");
                            var cheapestTicket = route.Tickets.OrderBy(t => t.Price).FirstOrDefault();
                            if (cheapestTicket != null)
                            {
                                sb.AppendLine($"    Giá vé từ: {cheapestTicket.Price.ToString("N0")} VND");
                            }
                        }
                    }
                }

                sb.AppendLine("\nVui lòng liên hệ quản trị viên để được gán vào trường học phù hợp.");
            }

            sb.AppendLine("\n**HƯỚNG DẪN SỬ DỤNG**");
            sb.AppendLine("- Hỏi về lịch trình: 'Lịch học của tôi ngày mai thế nào?'");
            sb.AppendLine("- Hỏi về tuyến xe: 'Tuyến xe nào đi qua quận 1?'");
            sb.AppendLine("- Hỏi về vé: 'Có các loại vé nào'");
            sb.AppendLine("- Hỗ trợ: 'Tôi muốn đăng ký vé tháng'");

            return sb.ToString();
        }

        private async Task<string> GetCurrentSemester(List<School> schools)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            foreach (var school in schools)
            {
                if (school.StartSemOne.HasValue && school.EndSemOne.HasValue &&
                    today >= school.StartSemOne.Value && today <= school.EndSemOne.Value)
                {
                    return "Học kỳ 1";
                }

                if (school.StartSemTwo.HasValue && school.EndSemTwo.HasValue &&
                    today >= school.StartSemTwo.Value && today <= school.EndSemTwo.Value)
                {
                    return "Học kỳ 2";
                }
            }

            return "Kỳ nghỉ";
        }

        private string GetTicketTypeName(TicketTypeEnum type)
        {
            return type switch
            {
                TicketTypeEnum.WEEKLY => "tuần",
                TicketTypeEnum.MONTHLY => "tháng",
                TicketTypeEnum.SEMESTER_ONE => "kỳ",
                TicketTypeEnum.SEMESTER_TWO => "kỳ",
                _ => type.ToString()
            };
        }

        private string GetCurrentSemester(School school)
        {
            if (school == null) return "Không xác định";

            var today = DateOnly.FromDateTime(DateTime.Now);

            if (school.StartSemOne.HasValue && school.EndSemOne.HasValue &&
                today >= school.StartSemOne.Value && today <= school.EndSemOne.Value)
            {
                return "Học kỳ 1";
            }

            if (school.StartSemTwo.HasValue && school.EndSemTwo.HasValue &&
                today >= school.StartSemTwo.Value && today <= school.EndSemTwo.Value)
            {
                return "Học kỳ 2";
            }

            return "Kỳ nghỉ";
        }

    }
}

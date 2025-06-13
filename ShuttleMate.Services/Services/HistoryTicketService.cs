using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.HistoryTicketModelView;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class HistoryTicketService : IHistoryTicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;
        private string _payOSApiUrl;
        private string _apiKey;
        private string _checksumKey;
        private string _clientKey; // Thêm biến lưu ClientKey
        private readonly ILogger<HistoryTicketService> _logger;
        private readonly HttpClient _httpClient;

        public HistoryTicketService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService, ILogger<HistoryTicketService> logger, HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _httpClient = httpClient;
            _payOSApiUrl = configuration["PayOS:ApiUrl"];
            _apiKey = configuration["PayOS:ApiKey"];
            _checksumKey = configuration["PayOS:ChecksumKey"];
            _clientKey = configuration["PayOS:ClientKey"];
        }

        public async Task<IEnumerable<HistoryTicketResponseModel>> GetAllForUserAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? ticketId = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            var historyTicketRepo = _unitOfWork.GetRepository<HistoryTicket>();

            var query = historyTicketRepo.Entities
                .Include(u => u.User)
                .Include(u => u.TicketType)
                .Where(x => x.UserId == cb)
                .AsQueryable();
            if (ticketId.HasValue)
            {
                query = query.Where(u => u.TicketId == ticketId);
            }
            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status);
            }
            if (PurchaseAt.HasValue)
            {
                query = query.Where(u => u.PurchaseAt.Date == PurchaseAt.Value.Date);
            }
            if (ValidFrom.HasValue)
            {
                query = query.Where(u => u.ValidFrom.Date == ValidFrom.Value.Date);
            }
            if (ValidUntil.HasValue)
            {
                query = query.Where(u => u.ValidUntil.Date == ValidUntil.Value.Date);
            }
            if (CreateTime == true)
            {
                query = query.OrderBy(x => x.CreatedTime);
            }
            else
            {
                query = query.OrderByDescending(x => x.CreatedTime);
            }

            var historyTickets = await query
                .Select(u => new HistoryTicketResponseModel
                {
                    Id = u.Id,
                    PurchaseAt = u.PurchaseAt,
                    ValidUntil = u.ValidUntil,
                    ValidFrom = u.ValidFrom,
                    TicketId = u.TicketId,
                    UserId = u.UserId,
                    Status = ConvertStatusToString(u.Status)
                })
                .ToListAsync();

            return historyTickets;
        }
        public async Task<IEnumerable<HistoryTicketResponseModel>> GetAllForAdminAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? userId = null, Guid? ticketId = null)
        {
            var historyTicketRepo = _unitOfWork.GetRepository<HistoryTicket>();

            var query = historyTicketRepo.Entities
                .Include(u => u.User)
                .Include(u => u.TicketType)
                .AsQueryable();
            if (userId.HasValue)
            {
                query = query.Where(u => u.UserId == userId);
            }
            if (ticketId.HasValue)
            {
                query = query.Where(u => u.TicketId == ticketId);
            }
            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status);
            }
            if (PurchaseAt.HasValue)
            {
                query = query.Where(u => u.PurchaseAt.Date == PurchaseAt.Value.Date);
            }
            if (ValidFrom.HasValue)
            {
                query = query.Where(u => u.ValidFrom.Date == ValidFrom.Value.Date);
            }
            if (ValidUntil.HasValue)
            {
                query = query.Where(u => u.ValidUntil.Date == ValidUntil.Value.Date);
            }
            if (CreateTime == true)
            {
                query = query.OrderBy(x => x.CreatedTime);
            }
            else
            {
                query = query.OrderByDescending(x => x.CreatedTime);
            }

            var historyTickets = await query
                .Select(u => new HistoryTicketResponseModel
                {
                    Id = u.Id,
                    PurchaseAt = u.PurchaseAt,
                    ValidUntil = u.ValidUntil,
                    ValidFrom = u.ValidFrom,
                    TicketId = u.TicketId,
                    UserId = u.UserId,
                    Status = ConvertStatusToString(u.Status)
                })
                .ToListAsync();

            return historyTickets;
        }
        private string ConvertStatusToString(HistoryTicketStatus status)
        {
            return status switch
            {
                HistoryTicketStatus.Book => "Đặt vé",
                HistoryTicketStatus.Paid => "Đã thanh toán",
                HistoryTicketStatus.Cancelled => "Hủy",
                _ => "Không xác định"
            };
        }
        public async Task<string> CreateHistoryTicket(CreateHistoryTicketModel model)
        {
            if (model.ValidFrom < DateTime.Now)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
            }
            if (model.ValidUntil < DateTime.Now)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
            }
            if (model.ValidFrom < model.ValidUntil)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu phải lớn hơn thời gian kết thúc");
            }
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            var ticketType = await _unitOfWork.GetRepository<TicketType>().Entities.FirstOrDefaultAsync(x => x.Id == model.TicketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");

            var historyTicket = new HistoryTicket
            {
                Id = Guid.NewGuid(),
                ValidFrom = model.ValidFrom,
                ValidUntil = model.ValidUntil,
                CreatedTime = DateTime.Now,
                TicketId = model.TicketId,
                Status = HistoryTicketStatus.Book,
                PurchaseAt = DateTime.Now,
                UserId = cb,
            };

            await _unitOfWork.GetRepository<HistoryTicket>().InsertAsync(historyTicket);
            await _unitOfWork.SaveAsync();

            // 2. Tạo PayOSPaymentRequest từ thông tin booking
            var payOSRequest = new PayOSPaymentRequest
            {
                orderCode = await GenerateUniqueOrderCodeAsync(),
                amount = (long)ticketType.Price, // Chuyển đổi TotalPrice sang long
                description = $"Thanh toán 100%!!!",
                buyerName = user.FullName, 
                buyerEmail = user.Email,   
                buyerPhone = user.PhoneNumber,
                buyerAddress = user.Address,  
                cancelUrl = "https://www.google.com/?hl=vi", 
                returnUrl = "https://www.google.com/?hl=vi", 
                expiredAt = DateTimeOffset.Now.ToUnixTimeSeconds() + 600,

                // ... các trường khác 
            };

            // 3. Tạo chữ ký
            payOSRequest.signature = CalculateSignature(payOSRequest);

            // 7. Tạo bản ghi Payment mới
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                PaymentMethod = PaymentMethodEnum.PayOs, 
                Status = PaymentStatus.Unpaid,
                Amount = ticketType.Price,
                OrderCode = payOSRequest.orderCode,
                BuyerAddress = payOSRequest.buyerAddress,
                Description = payOSRequest.description,
                Signature = payOSRequest.signature,
                BuyerEmail = payOSRequest.buyerEmail,
                BuyerPhone = payOSRequest.buyerPhone,
                BuyerName = payOSRequest.buyerName,
                CreatedBy = user.Id.ToString(),
                LastUpdatedBy = user.Id.ToString(),
                CreatedTime = DateTime.Now,
                LastUpdatedTime = DateTime.Now,
                HistoryTicketId = historyTicket.Id,
                //... các thông tin khác (nếu cần)...
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);

            await _unitOfWork.SaveAsync();
            // 4. Gọi API PayOS
            string checkoutUrl = await CallPayOSApi(payOSRequest);
            return checkoutUrl;
        }
        private string CalculateSignature(PayOSPaymentRequest request)
        {
            // 1. Đảm bảo amount là số nguyên
            int amount = (int)request.amount;

            // 2. Chỉ lấy các thông tin có trong dữ liệu PayOS gửi về (không có `cancelUrl`, `returnUrl`)
            string data = $"amount={amount}&cancelUrl={request.cancelUrl}&description={request.description}&orderCode={request.orderCode}&returnUrl={request.returnUrl}";

            Console.WriteLine($"Data to sign: {data}");

            // 3. Tạo HMAC-SHA256 signature
            byte[] keyBytes = Encoding.UTF8.GetBytes(_checksumKey);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(dataBytes);
                string signature = BitConverter.ToString(hash).Replace("-", "").ToLower();

                Console.WriteLine($"Generated signature: {signature}");
                return signature;
            }
        }
        private async Task<int> GenerateUniqueOrderCodeAsync()
        {
            Random random = new Random();
            int orderCode;
            bool exists;

            do
            {
                orderCode = random.Next(10000000, int.MaxValue); // Sinh số trong khoảng từ 8 chữ số đến 2.1 tỷ
                exists = await _unitOfWork.GetRepository<Transaction>().Entities
                    .AnyAsync(x => x.OrderCode == orderCode && !x.DeletedTime.HasValue);
            }
            while (exists);

            return orderCode;
        }
        private async Task<string> CallPayOSApi(PayOSPaymentRequest payOSRequest)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey); // Đổi từ "Authorization: Bearer" sang "x-api-key"
            _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientKey);

            string json = JsonConvert.SerializeObject(payOSRequest);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            using (HttpResponseMessage response = await _httpClient.PostAsync(_payOSApiUrl, content))
            {
                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    PayOSResponse payOSResponse = JsonConvert.DeserializeObject<PayOSResponse>(responseJson);

                    if (payOSResponse != null && payOSResponse.data != null && !string.IsNullOrEmpty(payOSResponse.data.checkoutUrl))
                    {
                        return payOSResponse.data.checkoutUrl;
                    }
                    else
                    {
                        throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, "Invalid PayOS response: " + responseJson);
                    }
                }
                else
                {
                    string errorJson = await response.Content.ReadAsStringAsync();
                    throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, $"Error calling PayOS API: {response.StatusCode} - {errorJson}");
                }
            }
        }


    }
}

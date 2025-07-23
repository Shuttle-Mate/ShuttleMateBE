using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
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
using static ShuttleMate.Services.Services.HistoryTicketService;

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
        private readonly ZaloPaySettings _zaloPaySettings;

        public HistoryTicketService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService, ILogger<HistoryTicketService> logger, HttpClient httpClient, IOptions<ZaloPaySettings> zaloPaySettings, IHttpClientFactory httpClientFactory)
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
            _zaloPaySettings = zaloPaySettings.Value;

        }

        #region payment PAYOS
        public async Task<BasePaginatedList<HistoryTicketResponseModel>> GetAllForUserAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateOnly? ValidFrom = null, DateOnly? ValidUntil = null, Guid? ticketId = null, string? ticketType = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            var historyTicketRepo = _unitOfWork.GetRepository<HistoryTicket>();

            var query = historyTicketRepo.Entities.Where(x => !x.DeletedTime.HasValue)
                .Include(u => u.User)
                .Include(u => u.Ticket)
                .Where(x => x.UserId == cb)
                .AsQueryable();
            if (ticketId.HasValue)
            {
                query = query.Where(u => u.TicketId == ticketId);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status.ToString().ToUpper() == status.ToUpper());
            }
            if (!string.IsNullOrWhiteSpace(ticketType))
            {
                query = query.Where(x => x.Ticket.Type.ToString().ToUpper() == ticketType.ToUpper());
            }
            if (PurchaseAt.HasValue)
            {
                query = query.Where(u => u.PurchaseAt.Date == PurchaseAt.Value.Date);
            }
            if (ValidFrom.HasValue)
            {
                query = query.Where(u => u.ValidFrom == ValidFrom.Value);
            }
            if (ValidUntil.HasValue)
            {
                query = query.Where(u => u.ValidUntil == ValidUntil.Value);
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
                    Status = u.Status.ToString().ToUpper(),
                    Price = u.Ticket.Price,
                    RouteName = u.Ticket.Route.RouteName,
                    Ticket = u.Ticket.Type.ToString().ToUpper(),
                    OrderCode = u.Transaction.OrderCode,
                })
                .ToListAsync();



            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<HistoryTicketResponseModel>(historyTickets, totalCount, page, pageSize);
        }
        public async Task<BasePaginatedList<HistoryTicketResponseModel>> GetAllForParentAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateOnly? ValidFrom = null, DateOnly? ValidUntil = null, Guid? ticketId = null, Guid? studentId = null, string? ticketType = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);
            var historyTicketRepo = _unitOfWork.GetRepository<HistoryTicket>();

            var query = historyTicketRepo.Entities.Where(x => !x.DeletedTime.HasValue)
                .Include(u => u.User)
                .Include(u => u.Ticket)
                .Where(x => x.User.ParentId == cb)
                .AsQueryable();
            if (ticketId.HasValue)
            {
                query = query.Where(u => u.TicketId == ticketId);
            }
            if (studentId.HasValue)
            {
                query = query.Where(u => u.User.Id == studentId);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status.ToString().ToUpper() == status!.ToUpper());
            }
            if (!string.IsNullOrWhiteSpace(ticketType))
            {
                query = query.Where(x => x.Ticket.Type.ToString().ToUpper() == ticketType!.ToUpper());
            }
            if (PurchaseAt.HasValue)
            {
                query = query.Where(u => u.PurchaseAt.Date == PurchaseAt.Value.Date);
            }
            if (ValidFrom.HasValue)
            {
                query = query.Where(u => u.ValidFrom == ValidFrom.Value);
            }
            if (ValidUntil.HasValue)
            {
                query = query.Where(u => u.ValidUntil == ValidUntil.Value);
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
                    Status = u.Status.ToString().ToUpper(),
                    Price = u.Ticket.Price,
                    RouteName = u.Ticket.Route.RouteName,
                    Ticket = u.Ticket.Type.ToString().ToUpper(),
                    OrderCode = u.Transaction.OrderCode,
                    ChildName = u.User.FullName,

                })
                .ToListAsync();

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<HistoryTicketResponseModel>(historyTickets, totalCount, page, pageSize);
        }
        public async Task<BasePaginatedList<HistoryTicketAdminResponseModel>> GetAllForAdminAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateOnly? ValidFrom = null, DateOnly? ValidUntil = null, Guid? userId = null, Guid? ticketId = null, string? ticketType = null)
        {
            var historyTicketRepo = _unitOfWork.GetRepository<HistoryTicket>();

            var query = historyTicketRepo.Entities.Where(x => !x.DeletedTime.HasValue)
                .Include(u => u.User)
                .Include(u => u.Ticket)
                .AsQueryable();
            if (userId.HasValue)
            {
                query = query.Where(u => u.UserId == userId);
            }
            if (ticketId.HasValue)
            {
                query = query.Where(u => u.TicketId == ticketId);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status.ToString().ToUpper() == status.ToUpper());
            }
            if (!string.IsNullOrWhiteSpace(ticketType))
            {
                query = query.Where(x => x.Ticket.Type.ToString().ToUpper() == ticketType.ToUpper());
            }
            if (PurchaseAt.HasValue)
            {
                query = query.Where(u => u.PurchaseAt.Date == PurchaseAt.Value.Date);
            }
            if (ValidFrom.HasValue)
            {
                query = query.Where(u => u.ValidFrom == ValidFrom.Value);
            }
            if (ValidUntil.HasValue)
            {
                query = query.Where(u => u.ValidUntil == ValidUntil.Value);
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
                .Select(u => new HistoryTicketAdminResponseModel
                {
                    Id = u.Id,
                    PurchaseAt = u.PurchaseAt,
                    ValidUntil = u.ValidUntil,
                    ValidFrom = u.ValidFrom,
                    TicketId = u.TicketId,
                    UserId = u.UserId,
                    Status = u.Status.ToString().ToUpper(),
                    Price = u.Ticket.Price,
                    RouteName = u.Ticket.Route.RouteName,
                    Ticket = u.Ticket.Type.ToString().ToUpper(),
                    FullNameOfUser = u.User.FullName,
                    OrderCode = u.Transaction.OrderCode,
                })
                .ToListAsync();

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<HistoryTicketAdminResponseModel>(historyTickets, totalCount, page, pageSize);
        }
        static string ConvertStatusToString(HistoryTicketStatus status)
        {
            return status switch
            {
                HistoryTicketStatus.UNPAID => "Đặt vé",
                HistoryTicketStatus.PAID => "Đã thanh toán",
                HistoryTicketStatus.CANCELLED => "Hủy",
                HistoryTicketStatus.USED => "Đã sử dụng",
                _ => "Không xác định"
            };
        }
        static string ConvertStatusTicketTypeToString(TicketTypeEnum status)
        {
            return status switch
            {
                TicketTypeEnum.SINGLE_RIDE => "Chuyến 1 chiều",
                TicketTypeEnum.DAY_PASS => "Chuyến trong ngày",
                TicketTypeEnum.WEEKLY => "Chuyến 1 tuần",
                TicketTypeEnum.SEMESTER_ONE => "Chuyến học kì 1",
                TicketTypeEnum.SEMESTER_TWO => "Chuyến học kì 2",
                TicketTypeEnum.MONTHLY => "Chuyến 1 tháng",
                _ => "Không xác định"
            };
        }
        public async Task<CreateHistoryTicketResponse> CreateHistoryTicket(CreateHistoryTicketModel model)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            var ticketType = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == model.TicketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");

            var historyTicket = new HistoryTicket
            {
                Id = Guid.NewGuid(),
                ValidFrom = model.ValidFrom,
                ValidUntil = model.ValidFrom,
                CreatedTime = DateTime.Now,
                TicketId = model.TicketId,
                Status = HistoryTicketStatus.UNPAID,
                PurchaseAt = DateTime.Now,
                UserId = cb,
                LastUpdatedTime = DateTime.Now,
                CreatedBy = userId
            };
            switch (ticketType.Type)
            {
                case TicketTypeEnum.SINGLE_RIDE:
                    if (model.ValidFrom < DateOnly.FromDateTime(DateTime.Now))
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom;
                    break;
                case TicketTypeEnum.DAY_PASS:
                    if (model.ValidFrom < DateOnly.FromDateTime(DateTime.Now))
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom;
                    break;
                case TicketTypeEnum.WEEKLY:
                    if (model.ValidFrom < DateOnly.FromDateTime(DateTime.Now))
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom.AddDays(7);
                    break;
                case TicketTypeEnum.MONTHLY:
                    if (model.ValidFrom < DateOnly.FromDateTime(DateTime.Now))
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom.AddMonths(1);
                    break;
                case TicketTypeEnum.SEMESTER_ONE:
                    historyTicket.ValidFrom = ticketType.Route.School.StartSemOne ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    historyTicket.ValidUntil = ticketType.Route.School.EndSemOne ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    break;
                case TicketTypeEnum.SEMESTER_TWO:
                    historyTicket.ValidFrom = ticketType.Route.School.StartSemTwo ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    historyTicket.ValidUntil = ticketType.Route.School.EndSemTwo ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!"); break;
            }

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
                PaymentMethod = PaymentMethodEnum.PAYOS,
                Status = PaymentStatus.UNPAID,
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
                DeletedBy = userId,
                //... các thông tin khác (nếu cần)...
            };

            await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);

            await _unitOfWork.SaveAsync();
            // 4. Gọi API PayOS
            PayOSResponseData checkoutUrl = await CallPayOSApi(payOSRequest);
            CreateHistoryTicketResponse response = new CreateHistoryTicketResponse
            {
                HistoryTicketId = historyTicket.Id,
                checkoutUrl = checkoutUrl.checkoutUrl,
                qrCode = checkoutUrl.qrCode,
                status = ConvertStatusToString(historyTicket.Status),
            };
            return response;
        }
        public async Task<string> ResponseHistoryTicketStatus(Guid historyTicketId)
        {
            if (string.IsNullOrWhiteSpace(historyTicketId.ToString()))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không được để trống Id của History Ticket!");
            }
            var historyTicket = await _unitOfWork.GetRepository<HistoryTicket>().Entities.FirstOrDefaultAsync(x => x.Id == historyTicketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy History Ticket!"); ;

            return historyTicket.Status.ToString().ToUpper();
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
        private async Task<PayOSResponseData> CallPayOSApi(PayOSPaymentRequest payOSRequest)
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
                        return payOSResponse.data;
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
        public async Task PayOSCallback(PayOSWebhookRequest request)
        {
            if (request?.data == null)
            {
                Console.WriteLine("Webhook request không có data, bỏ qua xử lý.");
                return;
            }

            // Tìm Transaction theo orderCode
            var transaction = await _unitOfWork.GetRepository<Transaction>().Entities
                .FirstOrDefaultAsync(x => x.OrderCode == request.data.orderCode && !x.DeletedTime.HasValue);


            if (transaction == null)
            {
                Console.WriteLine($"Không tìm thấy thanh toán với orderCode: {request.data.orderCode}. Bỏ qua xử lý.");
                return;
            }

            switch (request.data.code)
            {
                case "00": // Thành công

                    var user = await _unitOfWork.GetRepository<User>().Entities
                        .FirstOrDefaultAsync(x => x.Id.ToString() == transaction.CreatedBy && !x.DeletedTime.HasValue);

                    var historyTicket = await _unitOfWork.GetRepository<HistoryTicket>().Entities
                         .FirstOrDefaultAsync(x => x.Id == transaction.HistoryTicketId && !x.DeletedTime.HasValue);

                    historyTicket.Status = HistoryTicketStatus.PAID;
                    transaction.Status = PaymentStatus.PAID;

                    await _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
                    await _unitOfWork.GetRepository<HistoryTicket>().UpdateAsync(historyTicket);
                    await _unitOfWork.SaveAsync();

                    //Gửi mail hóa đơn thanh toán thành công 
                    await SendTicketPaymentSuccessEmail(user, historyTicket);
                    break;

                case "01": // Giao dịch thất bại
                    transaction.Status = PaymentStatus.UNPAID;
                    break;

                case "02": // Hủy giao dịch
                    transaction.Status = PaymentStatus.CANCELED;
                    break;

                default:
                    Console.WriteLine($"Trạng thái không xác định: {request.data.code}, bỏ qua xử lý.");
                    return;
            }

            await _unitOfWork.SaveAsync();
        }
        private async Task SendTicketPaymentSuccessEmail(User user, HistoryTicket historyTicket)
        {

            // Create email content
            await _emailService.SendEmailAsync(
                user.Email,
                "Xác Nhận Thanh Toán Vé Thành Công",
                $@"
                <html>
                <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 0;
                        }}
                        .container {{
                            width: 100%;
                            max-width: 600px;
                            margin: 20px auto;
                            background: #ffffff;
                            padding: 20px;
                            border-radius: 8px;
                            box-shadow: 0px 0px 10px rgba(0, 0, 0, 0.1);
                        }}
                        h2, h3 {{
                            color: #333;
                        }}
                        p {{
                            font-size: 16px;
                            line-height: 1.6;
                            color: #555;
                        }}
                        .section {{
                            margin-bottom: 20px;
                            padding-bottom: 10px;
                            border-bottom: 1px solid #ddd;
                        }}
                        .footer {{
                            margin-top: 20px;
                            font-size: 14px;
                            color: #777;
                            text-align: center;
                        }}
                        table {{
                            width: 100%;
                            border-collapse: collapse;
                        }}
                        table, th, td {{
                            border: 1px solid #ddd;
                        }}
                        th, td {{
                            padding: 10px;
                            text-align: left;
                        }}
                        th {{
                            background-color: #f8f8f8;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>XÁC NHẬN THANH TOÁN VÉ</h2>
                        <p><strong>Công ty Cổ phần ShuttleMate</strong></p>
                        <p>Số ĐKKD: XXXXXXXX</p>
                        <p>Địa chỉ: [Địa chỉ công ty]</p>
                        <p>Hotline: [Số hotline]</p>
                        <p>Email: shuttlemate.service@gmail.com</p>

                        <div class='section'>
                            <h3>THÔNG TIN THANH TOÁN</h3>
                            <p><strong>Mã vé:</strong> {historyTicket.Transaction.OrderCode}</p>
                            <p><strong>Ngày thanh toán:</strong> {historyTicket.PurchaseAt:dd/MM/yyyy}</p>
                            <p><strong>Ngày áp dụng:</strong> {historyTicket.ValidFrom:dd/MM/yyyy}</p>
                            <p><strong>Ngày hết hạn:</strong> {historyTicket.ValidUntil:dd/MM/yyyy}</p>
                            <p><strong>Hình thức thanh toán:</strong> {historyTicket.Transaction.PaymentMethod}</p>
                        </div>

                        <div class='section'>
                            <h3>THÔNG TIN KHÁCH HÀNG</h3>
                            <p><strong>Họ và tên:</strong> {user.FullName}</p>
                            <p><strong>Email:</strong> {user.Email}</p>
                            <p><strong>Số điện thoại:</strong> {user.PhoneNumber}</p>
                        </div>

                        <div class='section'>
                            <h3>CHI TIẾT VÉ</h3>
                            <table>
                                <thead>
                                    <tr>
                                        <th>Đường đi</th>
                                        <th>Loại vé</th>
                                        <th>Giá vé</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>{historyTicket.Ticket.Route.RouteName}</td>
                                        <td>{ConvertStatusTicketTypeToString(historyTicket.Ticket.Type)}</td>
                                        <td>{historyTicket.Ticket.Price:N0} đ</td>
                                    </tr>
                                </tbody>
                            </table>
                            <p><strong>Tổng tiền:</strong> {historyTicket.Ticket.Price:N0} đ</p>
                        </div>

                        <div class='footer'>
                            <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                        </div>
                    </div>
                </body>
                </html>"
            );

        }
        #endregion

        #region payment ZALOPAY
        public class ZaloPaySettings
        {
            public string PaymentUrl { get; set; }
            public int AppId { get; set; }
            public string AppUser { get; set; }
            public string Key1 { get; set; }
            public string Key2 { get; set; }
            public string RedirectUrl { get; set; }
            public string IpnUrl { get; set; }
        }

        public async Task<string> CreateZaloPayOrder(CreateZaloPayOrderModel model)
        {
            // Kiểm tra các điều kiện nhập liệu
            if (model.ValidFrom < DateOnly.FromDateTime(DateTime.Now))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
            }
            if (model.ValidUntil < DateOnly.FromDateTime(DateTime.Now))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
            }
            if (model.ValidFrom > model.ValidUntil)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu phải lớn hơn thời gian kết thúc");
            }

            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid cb);

            var ticketType = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == model.TicketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "User không tồn tại!");

            // Tạo HistoryTicket
            var historyTicket = new HistoryTicket
            {
                Id = Guid.NewGuid(),
                ValidFrom = model.ValidFrom,
                ValidUntil = model.ValidUntil,
                CreatedTime = DateTime.Now,
                TicketId = model.TicketId,
                Status = HistoryTicketStatus.UNPAID,
                PurchaseAt = DateTime.Now,
                UserId = cb,
                LastUpdatedTime = DateTime.Now,
                CreatedBy = userId
            };

            await _unitOfWork.GetRepository<HistoryTicket>().InsertAsync(historyTicket);
            await _unitOfWork.SaveAsync();

            // Tạo mã đơn hàng duy nhất từ hàm GenerateUniqueOrderCodeAsync
            var orderCode = await GenerateUniqueOrderCodeAsync();
            var appTransId = DateTime.Now.ToString("yyMMdd") + "_" + orderCode.ToString();

            var embeddata = "{\"promotioninfo\":\"\",\"merchantinfo\":\"" + userId + "\"}";

            var items = new[] {
        new { itemid = ticketType.Id, itemname = ConvertStatusTicketTypeToString(ticketType.Type), itemprice = ticketType.Price, itemquantity = 1 }
    };

            var param = new Dictionary<string, string>
            {
                { "appid", _zaloPaySettings.AppId.ToString() },
                { "appuser", _zaloPaySettings.AppUser },
                { "apptime", DateTime.Now.GetTimeStamp().ToString() },
                { "amount", ticketType.Price.ToString("0") },
                { "apptransid", appTransId },
                { "embeddata", embeddata },
                { "item", JsonConvert.SerializeObject(items) },
                { "description", "ZaloPay demo" },
                { "bankcode", "zalopayapp" },
                { "phone", user.PhoneNumber },
                { "email", user.Email },
                { "address", user.Address },
                { "subappid", "sub123" }
            };

            // Kiểm tra xem Key1 có null không
            if (string.IsNullOrEmpty(_zaloPaySettings.Key1))
            {
                throw new InvalidOperationException("Key1 không được null hoặc rỗng.");
            }

            // Tính toán HMAC
            var itemsJson = JsonConvert.SerializeObject(items);
            var data = _zaloPaySettings.AppId.ToString() + "|"
                       + appTransId + "|"
                       + _zaloPaySettings.AppUser + "|"
                       + ticketType.Price.ToString("0") + "|"
                       + DateTime.Now.GetTimeStamp() + "|"
                       + embeddata + "|"
                       + itemsJson;

            param["mac"] = ComputeHMACSHA256(data, _zaloPaySettings.Key1);

            // Gửi yêu cầu POST đến ZaloPay
            using var client = new HttpClient();
            var result = client.PostAsync(_zaloPaySettings.PaymentUrl, new FormUrlEncodedContent(param)).Result;

            if (result.IsSuccessStatusCode)
            {
                var responseString = result.Content.ReadAsStringAsync().Result;
                var response = JsonConvert.DeserializeObject<ZaloPayResponse>(responseString);

                if (response.returnCode == 1)
                {
                    var transaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        //PaymentMethod = PaymentMethodEnum.ZaloPay,
                        Status = PaymentStatus.UNPAID,
                        Amount = ticketType.Price,
                        OrderCode = orderCode,
                        BuyerAddress = user.Address,
                        Description = "Thanh toán ZaloPay",
                        Signature = param["mac"],
                        BuyerEmail = user.Email,
                        BuyerPhone = user.PhoneNumber,
                        BuyerName = user.FullName,
                        CreatedBy = user.Id.ToString(),
                        LastUpdatedBy = user.Id.ToString(),
                        CreatedTime = DateTime.Now,
                        LastUpdatedTime = DateTime.Now,
                        HistoryTicketId = historyTicket.Id,
                    };

                    await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
                    await _unitOfWork.SaveAsync();

                    return response.orderUrl;
                }
                else
                {
                    return response.returnMessage;
                }
            }
            else
            {
                return result.ReasonPhrase;
            }
        }


        private string ComputeHMACSHA256(string data, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        #endregion

    }
}

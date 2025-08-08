using AutoMapper;
using Azure;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using static System.Net.WebRequestMethods;

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
        private readonly IBackgroundJobClient _backgroundJobClient;
        public HistoryTicketService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService, ILogger<HistoryTicketService> logger, HttpClient httpClient, IOptions<ZaloPaySettings> zaloPaySettings, IHttpClientFactory httpClientFactory, IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _httpClient = httpClient;
            _payOSApiUrl = configuration["PayOS:ApiUrl"]!;
            _apiKey = configuration["PayOS:ApiKey"]!;
            _checksumKey = configuration["PayOS:ChecksumKey"]!;
            _clientKey = configuration["PayOS:ClientKey"]!;
            _zaloPaySettings = zaloPaySettings.Value;
            _backgroundJobClient = backgroundJobClient;
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

            var totalCount = await query.CountAsync();

            var pagedItems = await query
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
                    BuyerName = u.User.FullName,
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<HistoryTicketResponseModel>(pagedItems, totalCount, page, pageSize);
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

            var totalCount = await query.CountAsync();

            var pagedItems = await query
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
                     BuyerName = u.User.FullName,

                 })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<HistoryTicketResponseModel>(pagedItems, totalCount, page, pageSize);
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

            var totalCount = await query.CountAsync();

            var pagedItems = await query
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
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<HistoryTicketAdminResponseModel>(pagedItems, totalCount, page, pageSize);
        }
        static string ConvertStatusToString(HistoryTicketStatus status)
        {
            return status switch
            {
                HistoryTicketStatus.UNPAID => "Đặt vé",
                HistoryTicketStatus.PAID => "Đã thanh toán",
                HistoryTicketStatus.CANCELLED => "Hủy",
                _ => "Không xác định"
            };
        }
        static string ConvertStatusTicketTypeToString(TicketTypeEnum status)
        {
            return status switch
            {
                //TicketTypeEnum.SINGLE_RIDE => "Chuyến 1 chiều",
                //TicketTypeEnum.DAY_PASS => "Chuyến trong ngày",
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

            Guid targetUserId = model.StudentId ?? cb;

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == model.TicketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == targetUserId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy người dùng!");
            if (model.ListSchoolShiftId.Count == 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn ca học của bạn!");
            }
            var historyTicket = new HistoryTicket
            {
                Id = Guid.NewGuid(),
                ValidFrom = model.ValidFrom,
                ValidUntil = model.ValidFrom,
                CreatedTime = vietnamNow,
                TicketId = model.TicketId,
                Status = HistoryTicketStatus.UNPAID,
                PurchaseAt = vietnamNow,
                UserId = user.Id,
                LastUpdatedTime = vietnamNow,
                CreatedBy = userId
            };

            switch (ticket.Type)
            {
                case TicketTypeEnum.WEEKLY:
                    if (model.ValidFrom <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom.AddDays(7);
                    break;
                case TicketTypeEnum.MONTHLY:
                    if (model.ValidFrom <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom.AddMonths(1);
                    break;
                case TicketTypeEnum.SEMESTER_ONE:
                    if (ticket.Route.School.StartSemOne == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.EndSemOne == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.EndSemTwo < todayVN)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.StartSemOne <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Vé chỉ được mua trước ngày {ticket.Route.School.StartSemOne}!");
                    }

                    historyTicket.ValidFrom = ticket.Route.School.StartSemOne ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    historyTicket.ValidUntil = ticket.Route.School.EndSemOne ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    break;
                case TicketTypeEnum.SEMESTER_TWO:
                    if (ticket.Route.School.StartSemTwo == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày giờ cho vé này!");
                    }
                    if (ticket.Route.School.EndSemTwo == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.EndSemTwo <= todayVN)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    }
                    if (ticket.Route.School.StartSemTwo <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Vé chỉ được mua trước ngày {ticket.Route.School.StartSemTwo}!");
                    }
                    historyTicket.ValidFrom = ticket.Route.School.StartSemTwo ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    historyTicket.ValidUntil = ticket.Route.School.EndSemTwo ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!"); break;
            }

            //Xóa  
            var userShift = await _unitOfWork.GetRepository<UserSchoolShift>()
                .Entities
                .Where(x => x.StudentId == user.Id && !x.DeletedTime.HasValue).ToListAsync();
            // Xoá các userShift trước
            if (userShift != null)
            {
                foreach (var del in userShift)
                {
                    await _unitOfWork.GetRepository<UserSchoolShift>().DeleteAsync(del);
                }
                await _unitOfWork.SaveAsync();
            }

            // Tiếp tục insert
            foreach (var schoolShiftId in model.ListSchoolShiftId)
            {
                var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities
                    .FirstOrDefaultAsync(x => x.Id == schoolShiftId && !x.DeletedTime.HasValue)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại!");

                var userShiftNew = new UserSchoolShift
                {
                    Id = Guid.NewGuid(),
                    SchoolShiftId = schoolShiftId,
                    StudentId = user.Id,
                    CreatedTime = vietnamNow,
                    LastUpdatedTime = vietnamNow
                };

                await _unitOfWork.GetRepository<UserSchoolShift>().InsertAsync(userShiftNew);
            }

            await _unitOfWork.GetRepository<HistoryTicket>().InsertAsync(historyTicket);
            await _unitOfWork.SaveAsync();

            // 2. Tạo PayOSPaymentRequest từ thông tin booking
            var payOSRequest = new PayOSPaymentRequest
            {
                orderCode = await GenerateUniqueOrderCodeAsync(),
                amount = (long)ticket.Price, // Chuyển đổi TotalPrice sang long
                description = $"Thanh toán!!!",
                buyerName = user.FullName,
                buyerEmail = user.Email,
                buyerPhone = user.PhoneNumber,
                buyerAddress = user.Address!,
                cancelUrl = "https://www.google.com/?hl=vi",
                returnUrl = "https://www.google.com/?hl=vi",
                expiredAt = DateTimeOffset.Now.ToUnixTimeSeconds() + 900,

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
                Amount = ticket.Price,
                OrderCode = payOSRequest.orderCode,
                BuyerAddress = payOSRequest.buyerAddress,
                Description = payOSRequest.description,
                Signature = payOSRequest.signature,
                BuyerEmail = payOSRequest.buyerEmail,
                BuyerPhone = payOSRequest.buyerPhone,
                BuyerName = payOSRequest.buyerName,
                CreatedBy = userId,
                LastUpdatedBy = user.Id.ToString(),
                CreatedTime = vietnamNow,
                LastUpdatedTime = vietnamNow,
                HistoryTicketId = historyTicket.Id,
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
                status = historyTicket.Status.ToString().ToUpper(),
            };
            _backgroundJobClient.Schedule<HistoryTicketService>(
            service => service.ResponseHistoryTicketStatus(historyTicket.Id),
            TimeSpan.FromMinutes(10) // Delay 10 phút
        );
            return response;
        }
        public async Task<CreateHistoryTicketResponse> CreateHistoryTicketForParent(CreateHistoryTicketForParentModel model)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            //id cua phu huynh
            Guid.TryParse(userId, out Guid cb);
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);
            //kiểm tra id của phụ huynh
            var parent = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy người dùng!");
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == model.TicketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            //học sinh
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.StudentId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy học sinh!");
            if (model.ListSchoolShiftId.Count == 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn ca học của bạn!");
            }
            var historyTicket = new HistoryTicket
            {
                Id = Guid.NewGuid(),
                ValidFrom = model.ValidFrom,
                ValidUntil = model.ValidFrom,
                CreatedTime = vietnamNow,
                TicketId = model.TicketId,
                Status = HistoryTicketStatus.UNPAID,
                PurchaseAt = vietnamNow,
                UserId = user.Id,
                LastUpdatedTime = vietnamNow,
                CreatedBy = user.Id.ToString(),
            };

            switch (ticket.Type)
            {
                //case TicketTypeEnum.SINGLE_RIDE:
                //    if (model.ValidFrom <= DateOnly.FromDateTime(DateTime.Now))
                //    {
                //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                //    }
                //    historyTicket.ValidUntil = model.ValidFrom;
                //    break;
                //case TicketTypeEnum.DAY_PASS:
                //    if (model.ValidFrom <= todayVN)
                //    {
                //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                //    }
                //    historyTicket.ValidUntil = model.ValidFrom;
                //    break;
                case TicketTypeEnum.WEEKLY:
                    if (model.ValidFrom <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom.AddDays(7);
                    break;
                case TicketTypeEnum.MONTHLY:
                    if (model.ValidFrom <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể đặt thời gian trong quá khứ!");
                    }
                    historyTicket.ValidUntil = model.ValidFrom.AddMonths(1);
                    break;
                case TicketTypeEnum.SEMESTER_ONE:
                    if (ticket.Route.School.StartSemOne == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.EndSemOne == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.EndSemTwo < todayVN)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.StartSemOne <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Vé chỉ được mua trước ngày {ticket.Route.School.StartSemOne}!");
                    }

                    historyTicket.ValidFrom = ticket.Route.School.StartSemOne ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    historyTicket.ValidUntil = ticket.Route.School.EndSemOne ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    break;
                case TicketTypeEnum.SEMESTER_TWO:
                    if (ticket.Route.School.StartSemTwo == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày giờ cho vé này!");
                    }
                    if (ticket.Route.School.EndSemTwo == null)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp ngày cho vé này!");
                    }
                    if (ticket.Route.School.EndSemTwo <= todayVN)
                    {
                        await _emailService.SendEmailAsync(
                            ticket.Route.School.Email!,
                            "Thông báo: Cập nhật thời gian kỳ học",
                            "Kính gửi Quý Trường,<br><br>" +
                            "Vui lòng cập nhật thời gian <b>bắt đầu</b> và <b>kết thúc</b> kỳ học để hệ thống có thể kích hoạt vé kỳ    và     đảm     bảo     hoạt    động   bình thường.<br><br>" +
                            "Trân trọng."
                        );

                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    }
                    if (ticket.Route.School.StartSemTwo <= todayVN)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Vé chỉ được mua trước ngày {ticket.Route.School.StartSemTwo}!");
                    }
                    historyTicket.ValidFrom = ticket.Route.School.StartSemTwo ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!");
                    historyTicket.ValidUntil = ticket.Route.School.EndSemTwo ?? throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường tạm thời chưa xếp giờ cho vé này!"); break;
            }
            //xóa userShift
            var userShift = await _unitOfWork.GetRepository<UserSchoolShift>().Entities.Where(x => x.StudentId == user.Id && !x.DeletedTime.HasValue).ToListAsync();
            if (userShift != null)
            {
                foreach (var del in userShift)
                {
                    await _unitOfWork.GetRepository<UserSchoolShift>().DeleteAsync(del);
                }
                await _unitOfWork.SaveAsync();
            }

            foreach (var schoolShiftId in model.ListSchoolShiftId)
            {
                var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == schoolShiftId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại!");

                var userShiftNew = new UserSchoolShift
                {
                    Id = Guid.NewGuid(),
                    SchoolShiftId = schoolShiftId,
                    StudentId = user.Id,
                    CreatedTime = vietnamNow,
                    LastUpdatedTime = vietnamNow
                };
                await _unitOfWork.GetRepository<UserSchoolShift>().InsertAsync(userShiftNew);
            }

            await _unitOfWork.GetRepository<HistoryTicket>().InsertAsync(historyTicket);
            await _unitOfWork.SaveAsync();

            // 2. Tạo PayOSPaymentRequest từ thông tin booking
            var payOSRequest = new PayOSPaymentRequest
            {
                orderCode = await GenerateUniqueOrderCodeAsync(),
                amount = (long)ticket.Price, // Chuyển đổi TotalPrice sang long
                description = $"Thanh toán!!!",
                buyerName = user.FullName,
                buyerEmail = user.Email,
                buyerPhone = user.PhoneNumber,
                buyerAddress = user.Address!,
                cancelUrl = "https://www.google.com/?hl=vi",
                returnUrl = "https://www.google.com/?hl=vi",
                expiredAt = DateTimeOffset.Now.ToUnixTimeSeconds() + 600,

                // ... các trường khác 
            };

            // 3. Tạo chữ ký
            payOSRequest.signature = CalculateSignature(payOSRequest);

            // 7. Tạo bản ghi Payment mới
            //Lưu id của parent trong createBy để gửi mail trong api call back cho phụ huynh về hóa đơn vé
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                PaymentMethod = PaymentMethodEnum.PAYOS,
                Status = PaymentStatus.UNPAID,
                Amount = ticket.Price,
                OrderCode = payOSRequest.orderCode,
                BuyerAddress = payOSRequest.buyerAddress,
                Description = payOSRequest.description,
                Signature = payOSRequest.signature,
                BuyerEmail = payOSRequest.buyerEmail,
                BuyerPhone = payOSRequest.buyerPhone,
                BuyerName = payOSRequest.buyerName,
                CreatedBy = userId,
                LastUpdatedBy = user.Id.ToString(),
                CreatedTime = vietnamNow,
                LastUpdatedTime = vietnamNow,
                HistoryTicketId = historyTicket.Id,
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
                status = historyTicket.Status.ToString().ToUpper(),
            };

            return response;
        }
        public async Task<string> ResponseHistoryTicketStatus(Guid historyTicketId)
        {

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            if (historyTicketId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Id History Ticket không hợp lệ!");
            }

            var historyTicket = await _unitOfWork.GetRepository<HistoryTicket>().Entities
                .FirstOrDefaultAsync(x => x.Id == historyTicketId && !x.DeletedTime.HasValue);

            if (historyTicket == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy History Ticket!");
            }

            // Kiểm tra timeout nếu ticket đang UNPAID
            if (historyTicket.Status == HistoryTicketStatus.UNPAID)
            {
                var transaction = await _unitOfWork.GetRepository<Transaction>().Entities
                    .FirstOrDefaultAsync(t => t.HistoryTicketId == historyTicketId && !t.DeletedTime.HasValue);

                // Nếu quá 10 phút thời gian đã cài đặt
                if (transaction != null && vietnamNow > transaction.CreatedTime.AddMinutes(9).AddSeconds(55))
                {
                    historyTicket.Status = HistoryTicketStatus.CANCELLED;
                    transaction.Status = PaymentStatus.CANCELLED;

                    await _unitOfWork.GetRepository<HistoryTicket>().UpdateAsync(historyTicket);
                    await _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
                    await _unitOfWork.SaveAsync();
                }
            }

            return historyTicket.Status.ToString().ToUpper();
        }


        public async Task CancelTicket(Guid historyTicketId)
        {
            var historyTicket = await _unitOfWork.GetRepository<HistoryTicket>().Entities
                .FirstOrDefaultAsync(x => x.Id == historyTicketId && !x.DeletedTime.HasValue);

            if (historyTicket == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy vé!");
            }

            // Kiểm tra nếu đã thanh toán thì không được hủy
            if (historyTicket.Status == HistoryTicketStatus.PAID)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Không thể hủy ticket đã thanh toán!");
            }

            // Cập nhật trạng thái
            historyTicket.Status = HistoryTicketStatus.CANCELLED;

            // Cập nhật Transaction liên quan (nếu có)
            var transaction = await _unitOfWork.GetRepository<Transaction>().Entities
                .FirstOrDefaultAsync(t => t.HistoryTicketId == historyTicketId && !t.DeletedTime.HasValue);

            if (transaction != null)
            {
                transaction.Status = PaymentStatus.CANCELLED;
                await _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
            }

            await _unitOfWork.GetRepository<HistoryTicket>().UpdateAsync(historyTicket);
            await _unitOfWork.SaveAsync();

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
            var historyTicket = await _unitOfWork.GetRepository<HistoryTicket>().Entities
            .FirstOrDefaultAsync(x => x.Id == transaction.HistoryTicketId && !x.DeletedTime.HasValue);

            switch (request.data.code)
            {
                case "00": // Thành công

                    var user = await _unitOfWork.GetRepository<User>().Entities
                        .FirstOrDefaultAsync(x => x.Id.ToString() == transaction.CreatedBy
                        && x.Violate == false
                        && !x.DeletedTime.HasValue);


                    historyTicket.Status = HistoryTicketStatus.PAID;
                    transaction.Status = PaymentStatus.PAID;

                    await _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
                    await _unitOfWork.GetRepository<HistoryTicket>().UpdateAsync(historyTicket);
                    await _unitOfWork.SaveAsync();
                    if (user != null && user.UserRoles.Any(x => x.Role.Name.ToUpper() == "PARENT"))
                    {
                        //tìm
                        var child = await _unitOfWork.GetRepository<User>().Entities
                        .FirstOrDefaultAsync(x => x.ParentId == user.Id
                        && x.Violate == false
                        && !x.DeletedTime.HasValue);
                        if (child != null)
                        {
                            //Gửi mail hóa đơn thanh toán thành công 
                            await SendTicketPaymentSuccessEmailForParent(user, child, historyTicket);
                        }
                    }
                    else
                    {
                        //Gửi mail hóa đơn thanh toán thành công 
                        await SendTicketPaymentSuccessEmail(user, historyTicket);
                    }

                    break;

                case "01": // Giao dịch thất bại
                    transaction.Status = PaymentStatus.UNPAID;
                    historyTicket.Status = HistoryTicketStatus.UNPAID;
                    await _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
                    await _unitOfWork.GetRepository<HistoryTicket>().UpdateAsync(historyTicket);
                    await _unitOfWork.SaveAsync();
                    break;

                case "02": // Hủy giao dịch
                    transaction.Status = PaymentStatus.CANCELLED;
                    historyTicket.Status = HistoryTicketStatus.CANCELLED;
                    await _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
                    await _unitOfWork.GetRepository<HistoryTicket>().UpdateAsync(historyTicket);
                    await _unitOfWork.SaveAsync();
                    break;

                default:
                    Console.WriteLine($"Trạng thái không xác định: {request.data.code}, bỏ qua xử lý.");
                    return;
            }

            await _unitOfWork.SaveAsync();
        }
        private async Task SendTicketPaymentSuccessEmail(User user, HistoryTicket historyTicket)
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Xác Nhận Thanh Toán Vé Thành Công",
                $@"
        <html>
        <head>
            <style>
                body, p, h1, h2, h3, table, th, td, ul {{
                    margin: 0;
                    padding: 0;
                    font-family: 'Arial', sans-serif;
                }}
                
                body {{
                    background-color: #FAF9F7;
                    padding: 20px 0;
                    color: #333333;
                    line-height: 1.6;
                }}
                
                .email-container {{
                    max-width: 600px;
                    margin: 0 auto;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                }}
                
                .email-header {{
                    background-color: #124DA3;
                    padding: 25px 30px;
                    color: white;
                    text-align: center;
                }}
                
                .email-header h2 {{
                    font-size: 22px;
                    font-weight: 600;
                    margin-bottom: 10px;
                }}
                
                .company-info {{
                    padding: 20px 30px;
                    background-color: #FAF9F7;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .email-content {{
                    padding: 30px;
                }}
                
                .section {{
                    margin-bottom: 25px;
                }}
                
                .section h3 {{
                    color: #124DA3;
                    font-size: 18px;
                    margin-bottom: 15px;
                    padding-bottom: 8px;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .section p {{
                    margin-bottom: 10px;
                    font-size: 15px;
                }}
                
                table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 15px 0;
                    font-size: 15px;
                }}
                
                th {{
                    background-color: #124DA3;
                    color: white;
                    padding: 12px;
                    text-align: left;
                }}
                
                td {{
                    padding: 12px;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .total-amount {{
                    color: #F37022;
                    font-weight: bold;
                    font-size: 16px;
                    margin-top: 10px;
                }}
                
                .success-badge {{
                    display: inline-block;
                    background-color: #4EB748;
                    color: white;
                    padding: 4px 10px;
                    border-radius: 4px;
                    font-size: 14px;
                    font-weight: 500;
                    margin-left: 10px;
                }}
                
                .email-footer {{
                    background-color: #FAF9F7;
                    padding: 20px 30px;
                    text-align: center;
                    font-size: 14px;
                    color: #666666;
                }}
                
                .contact-info p {{
                    margin-bottom: 5px;
                }}
                
                .logo {{
                    font-weight: bold;
                    color: #124DA3;
                    font-size: 16px;
                    margin-bottom: 10px;
                }}
                
                @media only screen and (max-width: 600px) {{
                    .email-container {{
                        width: 100%;
                        border-radius: 0;
                    }}
                    
                    .email-header, .company-info, .email-content, .email-footer {{
                        padding: 20px;
                    }}
                    
                    th, td {{
                        padding: 8px;
                        font-size: 14px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='email-header'>
                    <h2>XÁC NHẬN THANH TOÁN VÉ <span class='success-badge'>THÀNH CÔNG</span></h2>
                </div>
                
                <div class='company-info'>
                    <p><strong>Công ty Cổ phần ShuttleMate</strong></p>
                    <p>Email: shuttlemate.service@gmail.com</p>
                </div>
                
                <div class='email-content'>
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
                        <p class='total-amount'>Tổng tiền: {historyTicket.Ticket.Price:N0} đ</p>
                    </div>
                </div>
                
                <div class='email-footer'>
                    <div class='logo'>SHUTTLEMATE</div>
                    <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                    <div class='contact-info'>
                        <p>Mọi thắc mắc vui lòng liên hệ:</p>
                        <p>Email: <a href='mailto:shuttlemate.service@gmail.com' style='color:#124DA3;'>shuttlemate.service@gmail.com</a></p>
                        <p>Hotline: 1900 1234</p>
                    </div>
                </div>
            </div>
        </body>
        </html>"
            );
        }
        private async Task SendTicketPaymentSuccessEmailForParent(User parent, User Child, HistoryTicket historyTicket)
        {
            await _emailService.SendEmailAsync(
                parent.Email,
                "Xác Nhận Thanh Toán Vé Cho Học Sinh Thành Công",
                $@"
        <html>
        <head>
            <style>
                body, p, h1, h2, h3, table, th, td, ul {{
                    margin: 0;
                    padding: 0;
                    font-family: 'Arial', sans-serif;
                }}
                
                body {{
                    background-color: #FAF9F7;
                    padding: 20px 0;
                    color: #333333;
                    line-height: 1.6;
                }}
                
                .email-container {{
                    max-width: 600px;
                    margin: 0 auto;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                }}
                
                .email-header {{
                    background-color: #124DA3;
                    padding: 25px 30px;
                    color: white;
                    text-align: center;
                }}
                
                .email-header h2 {{
                    font-size: 22px;
                    font-weight: 600;
                    margin-bottom: 10px;
                }}
                
                .company-info {{
                    padding: 20px 30px;
                    background-color: #FAF9F7;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .email-content {{
                    padding: 30px;
                }}
                
                .section {{
                    margin-bottom: 25px;
                }}
                
                .section h3 {{
                    color: #124DA3;
                    font-size: 18px;
                    margin-bottom: 15px;
                    padding-bottom: 8px;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .section p {{
                    margin-bottom: 10px;
                    font-size: 15px;
                }}
                
                table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 15px 0;
                    font-size: 15px;
                }}
                
                th {{
                    background-color: #124DA3;
                    color: white;
                    padding: 12px;
                    text-align: left;
                }}
                
                td {{
                    padding: 12px;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .total-amount {{
                    color: #F37022;
                    font-weight: bold;
                    font-size: 16px;
                    margin-top: 10px;
                }}
                
                .success-badge {{
                    display: inline-block;
                    background-color: #4EB748;
                    color: white;
                    padding: 4px 10px;
                    border-radius: 4px;
                    font-size: 14px;
                    font-weight: 500;
                    margin-left: 10px;
                }}
                
                .email-footer {{
                    background-color: #FAF9F7;
                    padding: 20px 30px;
                    text-align: center;
                    font-size: 14px;
                    color: #666666;
                }}
                
                .contact-info p {{
                    margin-bottom: 5px;
                }}
                
                .logo {{
                    font-weight: bold;
                    color: #124DA3;
                    font-size: 16px;
                    margin-bottom: 10px;
                }}
                
                @media only screen and (max-width: 600px) {{
                    .email-container {{
                        width: 100%;
                        border-radius: 0;
                    }}
                    
                    .email-header, .company-info, .email-content, .email-footer {{
                        padding: 20px;
                    }}
                    
                    th, td {{
                        padding: 8px;
                        font-size: 14px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='email-header'>
                    <h2>XÁC NHẬN THANH TOÁN VÉ <span class='success-badge'>THÀNH CÔNG</span></h2>
                </div>
                
                <div class='company-info'>
                    <p><strong>Công ty Cổ phần ShuttleMate</strong></p>
                    <p>Email: shuttlemate.service@gmail.com</p>
                </div>
                
                <div class='email-content'>
                    <div class='section'>
                        <h3>THÔNG TIN THANH TOÁN</h3>
                        <p><strong>Mã vé:</strong> {historyTicket.Transaction.OrderCode}</p>
                        <p><strong>Ngày thanh toán:</strong> {historyTicket.PurchaseAt:dd/MM/yyyy}</p>
                        <p><strong>Ngày áp dụng:</strong> {historyTicket.ValidFrom:dd/MM/yyyy}</p>
                        <p><strong>Ngày hết hạn:</strong> {historyTicket.ValidUntil:dd/MM/yyyy}</p>
                        <p><strong>Hình thức thanh toán:</strong> {historyTicket.Transaction.PaymentMethod}</p>
                    </div>
                    
                    <div class='section'>
                        <h3>THÔNG TIN KHÁCH HÀNG SỬ DỤNG</h3>
                        <p><strong>Họ và tên:</strong> {Child.FullName}</p>
                        <p><strong>Email:</strong> {Child.Email}</p>
                        <p><strong>Số điện thoại:</strong> {Child.PhoneNumber}</p>
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
                        <p class='total-amount'>Tổng tiền: {historyTicket.Ticket.Price:N0} đ</p>
                    </div>
                </div>
                
                <div class='email-footer'>
                    <div class='logo'>SHUTTLEMATE</div>
                    <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                    <div class='contact-info'>
                        <p>Mọi thắc mắc vui lòng liên hệ:</p>
                        <p>Email: <a href='mailto:shuttlemate.service@gmail.com' style='color:#124DA3;'>shuttlemate.service@gmail.com</a></p>
                        <p>Hotline: 1900 1234</p>
                    </div>
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

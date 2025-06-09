using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public HistoryTicketService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
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
        public async Task CreateHistoryTicket(CreateHistoryTicketModel model)
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

            var historyTicket = new HistoryTicket
            {
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
        }


    }
}

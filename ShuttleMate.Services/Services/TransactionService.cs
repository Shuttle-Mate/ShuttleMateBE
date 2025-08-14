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
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.TransactionModelView;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;

        public TransactionService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
        }
        public async Task<BasePaginatedList<TransactionResponseModel>> GetAllForUserAsync(int page = 0, int pageSize = 10,  string? paymentStatus = null, int? orderCode = null, string? description = null, bool? CreateTime = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            var transaction = _unitOfWork.GetRepository<Transaction>();

            var query = transaction.Entities.Where(x => !x.DeletedTime.HasValue)
                .Include(u => u.HistoryTicket)
                .Where(x => x.HistoryTicket.UserId == cb)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(description))
            {
                query = query.Where(u => u.Description.Contains(description));
            }
            if (orderCode.HasValue)
            {
                query = query.Where(u => u.OrderCode == orderCode);
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(u => u.Status.ToString().ToUpper() == paymentStatus.ToUpper());
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
                .Select(u => new TransactionResponseModel
                {
                    Id = u.Id,
                    Amount = u.Amount,
                    Status = u.Status.ToString().ToUpper(),
                    PaymentMethod = u.PaymentMethod.ToString().ToUpper(),
                    OrderCode = u.OrderCode,
                    Description = u.Description,
                    HistoryTicketId = u.HistoryTicketId,
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<TransactionResponseModel>(pagedItems, totalCount, page, pageSize);
        }
        public async Task<TransactionResponseModel> GetById(Guid transactionId)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);


            var query =await  _unitOfWork.GetRepository<Transaction>().Entities.FirstOrDefaultAsync(x => x.Id == transactionId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy giao dịch!");

            var transaction = new TransactionResponseModel
            {
                Id = query.Id,
                Amount = query.Amount,
                Status = query.Status.ToString().ToUpper(),
                PaymentMethod = query.PaymentMethod.ToString().ToUpper(),
                OrderCode = query.OrderCode,
                Description = query.Description,
                HistoryTicketId = query.HistoryTicketId,          
            };
                
            return transaction;
        }
        public async Task<BasePaginatedList<TransactionAdminResponseModel>> GetAllForAdminAsync(int page = 0, int pageSize = 10,  string? paymentStatus = null, int? orderCode = null, string? description = null, bool? CreateTime = null, Guid? userId = null)
        {
            var transaction = _unitOfWork.GetRepository<Transaction>();

            var query = transaction.Entities.Where(x => !x.DeletedTime.HasValue)
                .Include(u => u.HistoryTicket)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(description))
            {
                query = query.Where(u => u.Description.Contains(description));
            }
            if (orderCode.HasValue)
            {
                query = query.Where(u => u.OrderCode == orderCode);
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(u => u.Status == Enum.Parse<PaymentStatus>(paymentStatus));
            }
            if (CreateTime == true)
            {
                query = query.OrderBy(x => x.CreatedTime);
            }
            else
            {
                query = query.OrderByDescending(x => x.CreatedTime);
            }
            if(userId != null)
            {
                query = query.Where(x => x.HistoryTicket.UserId == userId);
            }

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Select(u => new TransactionAdminResponseModel
                {
                    Id = u.Id,
                    Amount = u.Amount,
                    Status = u.Status.ToString().ToUpper(),
                    PaymentMethod = u.PaymentMethod.ToString().ToUpper(),
                    OrderCode = u.OrderCode,
                    Description = u.Description,
                    HistoryTicketId = u.HistoryTicketId,
                    BuyerAddress = u.BuyerAddress,
                    BuyerEmail = u.BuyerEmail,
                    BuyerName = u.BuyerName,
                    BuyerPhone = u.BuyerPhone
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();


            return new BasePaginatedList<TransactionAdminResponseModel>(pagedItems, totalCount, page, pageSize); ;
        }

    }
}

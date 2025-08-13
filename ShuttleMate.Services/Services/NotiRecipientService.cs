using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.NotiRecipientModelView;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class NotiRecipientService : INotiRecipientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public NotiRecipientService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateNotiRecipient(NotiRecipientModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var notiRecipient = _mapper.Map<NotificationRecipient>(model);
            notiRecipient.Status = NotificationStatusEnum.PENDING;
            notiRecipient.CreatedBy = userId;
            notiRecipient.LastUpdatedBy = userId;
            await _unitOfWork.GetRepository<NotificationRecipient>().InsertAsync(notiRecipient);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteNotiRecipient(Guid notiRecipeientId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            NotificationRecipient notificationRecipient = await _unitOfWork.GetRepository<NotificationRecipient>().Entities.FirstOrDefaultAsync(x => x.Id == notiRecipeientId);
            var notiRecipient = await _unitOfWork.GetRepository<NotificationRecipient>().Entities.FirstOrDefaultAsync(x => x.Id == notiRecipeientId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông báo người dùng!");
            notiRecipient.DeletedTime = DateTime.Now;
            notiRecipient.DeletedBy = userId;
            await _unitOfWork.GetRepository<NotificationRecipient>().UpdateAsync(notiRecipient);
            await _unitOfWork.SaveAsync();
        }

        public async Task<BasePaginatedList<ResponseNotiRecipientModel>> GetAll(GetNotiRecipQuery req)
        {
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            IQueryable<NotificationRecipient> query = _unitOfWork.GetRepository<NotificationRecipient>()
                .Entities
                .Include(x => x.Recipient)
                .Include(x => x.Notification)
                .Where(x => !x.DeletedTime.HasValue);

            // Filter by status (string to enum, upper-case)
            //if (!string.IsNullOrWhiteSpace(req.status))
            //{
            //    if (Enum.TryParse<NotificationStatusEnum>(req.status.Trim().ToUpperInvariant(), out var statusEnum))
            //    {
            //        query = query.Where(x => x.Status == statusEnum);
            //    }
            //    else
            //    {
            //        throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trạng thái thông báo không hợp lệ!");
            //    }
            //}

            if (req.status != null)
            {
                query = query.Where(x => x.Status == req.status);
            }

            if (req.userId.HasValue && req.userId.Value != Guid.Empty)
            {
                query = query.Where(x => x.RecipientId == req.userId.Value);
            }

            if (req.notificationCategory != null)
            {
                query = query.Where (x => x.NotificationCategory == req.notificationCategory);
            }

            query = query.OrderBy(x => x.CreatedTime);

            var totalCount = query.Count();

            //Paging
            var notiRecipients = await query
                .Skip(req.page * req.pageSize)
                .Take(req.pageSize)
                .ToListAsync();

            if (!notiRecipients.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có thông báo nào tồn tại!");
            }

            var result = _mapper.Map<List<ResponseNotiRecipientModel>>(notiRecipients);

            return new BasePaginatedList<ResponseNotiRecipientModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseNotiRecipientModel> GetById(Guid notiRecipientId)
        {
            var notiRecipient = await _unitOfWork.GetRepository<NotificationRecipient>().Entities.FirstOrDefaultAsync(x => x.Id == notiRecipientId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông báo người dùng!");

            return _mapper.Map<ResponseNotiRecipientModel>(notiRecipient);
        }

        public async Task UpdateStatusNotiRecipient(UpdateNotiRecipientModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (!Enum.IsDefined(typeof(NotificationStatusEnum), model.Status))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, $"Giá trị {model.Status} không hợp lệ");
            }
            var notiRecipient = await _unitOfWork.GetRepository<NotificationRecipient>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông báo của người dùng!");

            //route = _mapper.Map<Route>(model);
            _mapper.Map(model, notiRecipient);
            notiRecipient.LastUpdatedBy = userId;
            notiRecipient.LastUpdatedTime = DateTime.Now;
            await _unitOfWork.GetRepository<NotificationRecipient>().UpdateAsync(notiRecipient);
            await _unitOfWork.SaveAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.NotiRecipientModelView;
using ShuttleMate.Services.Services.Infrastructure;
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
            notiRecipient.Status = NotificationStatusEnum.Pending;
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

        public async Task<List<ResponseNotiRecipientModel>> GetAll()
        {
            var notiRecipients = await _unitOfWork.GetRepository<NotificationRecipient>().Entities.Where(x => !x.DeletedTime.HasValue).OrderBy(x => x.CreatedTime).ToListAsync();
            if (!notiRecipients.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có thông báo nào tồn tại!");
            }
            return _mapper.Map<List<ResponseNotiRecipientModel>>(notiRecipients);
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

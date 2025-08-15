using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.NotiTemplateModelView;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public NotificationTemplateService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateNotiTemplate(NotiTemplateModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            NotificationTemplate notiTemplate = await _unitOfWork.GetRepository<NotificationTemplate>().Entities.FirstOrDefaultAsync(x => x.Type == model.Type || x.Template == model.Template);
            if (notiTemplate != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Mẫu thông báo này đã tồn tại!!");
            }

            var newNotiTemplate = _mapper.Map<NotificationTemplate>(model);
            newNotiTemplate.CreatedBy = userId;
            newNotiTemplate.LastUpdatedBy = userId;
            await _unitOfWork.GetRepository<NotificationTemplate>().InsertAsync(newNotiTemplate);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteNotiTemplate(Guid notiTempId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var notiTemplate = await _unitOfWork.GetRepository<NotificationTemplate>()
                .Entities
                .FirstOrDefaultAsync(x => x.Id == notiTempId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy mẫu thông báo!");

            notiTemplate.DeletedTime = DateTime.Now;
            notiTemplate.DeletedBy = userId;
            await _unitOfWork.GetRepository<NotificationTemplate>().UpdateAsync(notiTemplate);
            await _unitOfWork.SaveAsync();
        }

        public async Task<BasePaginatedList<ResponseNotiTemplateModel>> GetAll(GetNotiTemplateQuery req)
        {
            string searchKeyword = req.search ?? "";
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var notiTemplates = _unitOfWork.GetRepository<NotificationTemplate>()
                .Entities
                .Where(x => !x.DeletedTime.HasValue);
            
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                notiTemplates = notiTemplates.Where(x =>
                    x.Type.ToLower().Contains(searchKeyword.ToLower()) ||
                    x.Template.ToLower().Contains(searchKeyword.ToLower()));
            }

            var totalCount = notiTemplates.Count();

            //Paging
            var notiTemps = await notiTemplates
                .Skip(req.page * req.pageSize)
                .Take(req.pageSize)
                .ToListAsync();

            if (!notiTemps.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có mẫu thông báo nào tồn tại!");
            }

            var result = _mapper.Map<List<ResponseNotiTemplateModel>>(notiTemps);

            return new BasePaginatedList<ResponseNotiTemplateModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseNotiTemplateModel> GetById(Guid notiTempId)
        {
            var notiTemplate = await _unitOfWork.GetRepository<NotificationTemplate>()
                .Entities
                .FirstOrDefaultAsync(x => x.Id == notiTempId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy mẫu thông báo!");

            return _mapper.Map<ResponseNotiTemplateModel>(notiTemplate);
        }

        public async Task UpdateNotiTemplate(UpdateNotiTemplateModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (string.IsNullOrWhiteSpace(model.Type))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Loại của mẫu thông báo không được để trống!");
            }

            if (string.IsNullOrWhiteSpace(model.Template))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Mẫu thông báo không được để trống!");
            }

            var notificationTemplate = await _unitOfWork.GetRepository<NotificationTemplate>()
                .Entities
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) 
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy mẫu thông báo!");

            //route = _mapper.Map<Route>(model);
            _mapper.Map(model, notificationTemplate);
            notificationTemplate.LastUpdatedBy = userId;
            notificationTemplate.LastUpdatedTime = DateTime.Now;
            await _unitOfWork.GetRepository<NotificationTemplate>().UpdateAsync(notificationTemplate);
            await _unitOfWork.SaveAsync();
        }
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Http;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.ResponseSupportModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class ResponseSupportService : IResponseSupportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public ResponseSupportService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateAsync(CreateResponseSupportModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(model.SupportRequestId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            if (supportRequest.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị xóa.");

            if (supportRequest.Status == SupportRequestStatusEnum.RESOLVED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã được giải quyết.");

            if (supportRequest.Status == SupportRequestStatusEnum.CANCELLED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị hủy.");

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tiêu đề.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả.");
            }

            var newResponseSupport = _mapper.Map<ResponseSupport>(model);
            newResponseSupport.CreatedBy = userId;
            newResponseSupport.LastUpdatedBy = userId;

            supportRequest.Status = SupportRequestStatusEnum.RESPONDED;

            await _unitOfWork.GetRepository<ResponseSupport>().InsertAsync(newResponseSupport);
            await _unitOfWork.GetRepository<SupportRequest>().UpdateAsync(supportRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var responseSupportRepo = _unitOfWork.GetRepository<ResponseSupport>();
            var supportRequestRepo = _unitOfWork.GetRepository<SupportRequest>();

            var responseSupport = await responseSupportRepo.GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phản hồi hỗ trợ không tồn tại.");

            if (responseSupport.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phản hồi hỗ trợ đã bị xóa.");
            }

            responseSupport.LastUpdatedTime = CoreHelper.SystemTimeNow;
            responseSupport.LastUpdatedBy = userId;
            responseSupport.DeletedTime = CoreHelper.SystemTimeNow;
            responseSupport.DeletedBy = userId;

            await responseSupportRepo.UpdateAsync(responseSupport);

            var remainingResponses = await responseSupportRepo
                .FindAllAsync(rs =>
                    rs.SupportRequestId == responseSupport.SupportRequestId &&
                    !rs.DeletedTime.HasValue &&
                    rs.Id != id);

            if (!remainingResponses.Any())
            {
                var supportRequest = await supportRequestRepo.GetByIdAsync(responseSupport.SupportRequestId);
                if (supportRequest != null && !supportRequest.DeletedTime.HasValue)
                {
                    supportRequest.Status = SupportRequestStatusEnum.IN_PROGRESS;
                    supportRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
                    supportRequest.LastUpdatedBy = userId;

                    await supportRequestRepo.UpdateAsync(supportRequest);
                }
            }

            await _unitOfWork.SaveAsync();
        }
    }
}

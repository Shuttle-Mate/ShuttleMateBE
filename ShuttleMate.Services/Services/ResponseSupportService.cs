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

        public async Task<ResponseResponseSupportModel> GetByIdAsync(Guid id)
        {
            var responseSupport = await _unitOfWork.GetRepository<ResponseSupport>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phản hồi hỗ trợ không tồn tại.");

            if (responseSupport.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phản hồi hỗ trợ đã bị xóa.");
            }

            return _mapper.Map<ResponseResponseSupportModel>(responseSupport);
        }

        public async Task CreateAsync(CreateResponseSupportModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            if (model.SupportRequestId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền Id yêu cầu hỗ trợ hợp lệ.");
            }

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(model.SupportRequestId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            if (supportRequest.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị xóa.");
            }

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

            await _unitOfWork.GetRepository<ResponseSupport>().InsertAsync(newResponseSupport);

            supportRequest.Status = SupportRequestStatusEnum.RESPONSED;

            await _unitOfWork.GetRepository<SupportRequest>().UpdateAsync(supportRequest);

            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var responseSupport = await _unitOfWork.GetRepository<ResponseSupport>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phản hồi hỗ trợ không tồn tại.");

            if (responseSupport.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phản hồi hỗ trợ đã bị xóa.");
            }

            responseSupport.LastUpdatedTime = CoreHelper.SystemTimeNow;
            responseSupport.LastUpdatedBy = userId;
            responseSupport.DeletedTime = CoreHelper.SystemTimeNow;
            responseSupport.DeletedBy = userId;

            await _unitOfWork.GetRepository<ResponseSupport>().UpdateAsync(responseSupport);
            await _unitOfWork.SaveAsync();
        }
    }
}

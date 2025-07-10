using AutoMapper;
using Microsoft.AspNetCore.Http;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.ResponseSupportModelViews;
using ShuttleMate.ModelViews.SupportRequestModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public SupportRequestService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<IEnumerable<ResponseSupportRequestModel>> GetAllAsync()
        {
            var supportRequests = await _unitOfWork.GetRepository<SupportRequest>().FindAllAsync(a => !a.DeletedTime.HasValue);

            if (!supportRequests.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có yêu cầu hỗ trợ nào.");
            }

            return _mapper.Map<IEnumerable<ResponseSupportRequestModel>>(supportRequests);
        }

        public async Task<IEnumerable<ResponseSupportRequestModel>> GetAllMyAsync()
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var mySupportRequests = await _unitOfWork.GetRepository<SupportRequest>().FindAllAsync(a => !a.DeletedTime.HasValue && a.UserId.ToString() == userId);

            if (!mySupportRequests.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có yêu cầu hỗ trợ nào.");
            }

            return _mapper.Map<IEnumerable<ResponseSupportRequestModel>>(mySupportRequests);
        }

        public async Task<IEnumerable<ResponseResponseSupportModel>> GetAllMyResponseAsync(Guid id)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            if (supportRequest.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị xóa.");
            }

            var responseSupports = await _unitOfWork.GetRepository<ResponseSupport>().FindAllAsync(rs => rs.SupportRequestId == id);

            if (!responseSupports.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có phản hồi yêu cầu nào.");
            }

            return _mapper.Map<IEnumerable<ResponseResponseSupportModel>>(responseSupports);
        }

        public async Task<ResponseSupportRequestModel> GetByIdAsync(Guid id)
        {
            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            if (supportRequest.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị xóa.");
            }

            return _mapper.Map<ResponseSupportRequestModel>(supportRequest);
        }

        public async Task CreateAsync(CreateSupportRequestModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            if (!Enum.TryParse<SupportRequestCategoryEnum>(model.Category, true, out var categoryEnum) || !Enum.IsDefined(typeof(SupportRequestCategoryEnum), categoryEnum))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại yêu cầu hỗ trợ không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tiêu đề yêu cầu hỗ trợ.");
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền nội dung yêu cầu hỗ trợ.");
            }

            var newSupportRequest = _mapper.Map<SupportRequest>(model);

            newSupportRequest.Category = categoryEnum;
            newSupportRequest.Status = SupportRequestStatusEnum.IN_PROGRESS;
            newSupportRequest.UserId = userIdGuid;
            newSupportRequest.CreatedBy = userId;
            newSupportRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<SupportRequest>().InsertAsync(newSupportRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task EscalateAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            if (supportRequest.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị xóa.");
            }

            supportRequest.Status = SupportRequestStatusEnum.ESCALATED;
            supportRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            supportRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<SupportRequest>().UpdateAsync(supportRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task ResolveAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            if (supportRequest.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị xóa.");
            }

            supportRequest.Status = SupportRequestStatusEnum.RESOLVED;
            supportRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            supportRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<SupportRequest>().UpdateAsync(supportRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task CancelAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            if (supportRequest.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ đã bị xóa.");
            }

            supportRequest.Status = SupportRequestStatusEnum.CANCELLED;
            supportRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            supportRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<SupportRequest>().UpdateAsync(supportRequest);
            await _unitOfWork.SaveAsync();
        }
    }
}

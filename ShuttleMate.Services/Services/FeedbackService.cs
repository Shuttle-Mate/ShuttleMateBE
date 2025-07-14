using AutoMapper;
using Microsoft.AspNetCore.Http;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.FeedbackModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public FeedbackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<IEnumerable<ResponseFeedbackModel>> GetAllAdminAsync()
        {
            var feedbacks = await _unitOfWork.GetRepository<Feedback>().FindAllAsync(a => !a.DeletedTime.HasValue);

            if (!feedbacks.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có đánh giá nào.");
            }

            return _mapper.Map<IEnumerable<ResponseFeedbackModel>>(feedbacks);
        }

        public async Task<IEnumerable<ResponseFeedbackModel>> GetAllMyAsync()
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var myFeedbacks = await _unitOfWork.GetRepository<Feedback>().FindAllAsync(a => !a.DeletedTime.HasValue && a.UserId.ToString() == userId);

            if (!myFeedbacks.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có đánh giá nào.");
            }

            return _mapper.Map<IEnumerable<ResponseFeedbackModel>>(myFeedbacks);
        }

        public async Task<ResponseFeedbackModel> GetByIdAsync(Guid id)
        {
            var feedback = await _unitOfWork.GetRepository<Feedback>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá không tồn tại.");

            if (feedback.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá đã bị xóa.");
            }

            return _mapper.Map<ResponseFeedbackModel>(feedback);
        }

        public async Task CreateAsync(CreateFeedbackModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            if (!Enum.TryParse<FeedbackCategoryEnum>(model.FeedbackCategory, true, out var categoryEnum) || !Enum.IsDefined(typeof(FeedbackCategoryEnum), categoryEnum))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại phản hồi không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền thông điệp phản hồi.");
            }

            if (model.Rating < 1 || model.Rating > 5)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Đánh giá phải nằm trong khoảng từ 1 đến 5.");
            }

            var newFeedback = _mapper.Map<Feedback>(model);

            newFeedback.FeedbackCategory = categoryEnum;
            newFeedback.UserId = userIdGuid;
            newFeedback.CreatedBy = userId;
            newFeedback.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<Feedback>().InsertAsync(newFeedback);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var feedback = await _unitOfWork.GetRepository<Feedback>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá không tồn tại.");

            if (feedback.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá đã bị xóa.");
            }

            feedback.LastUpdatedTime = CoreHelper.SystemTimeNow;
            feedback.LastUpdatedBy = userId;
            feedback.DeletedTime = CoreHelper.SystemTimeNow;
            feedback.DeletedBy = userId;

            await _unitOfWork.GetRepository<Feedback>().UpdateAsync(feedback);
            await _unitOfWork.SaveAsync();
        }
    }
}

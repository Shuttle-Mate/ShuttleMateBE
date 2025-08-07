using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

        public async Task<BasePaginatedList<ResponseFeedbackModel>> GetAllAsync(
            string? search,
            string? category,
            DateOnly? from,
            DateOnly? to,
            Guid? userId,
            Guid? tripId,
            int? minRating,
            int? maxRating,
            bool sortAsc = false,
            int page = 0,
            int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<Feedback>()
                .GetQueryable()
                .Include(f => f.User)
                .Include(f => f.Trip)
                .Where(f => !f.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(f => f.Message.ToLower().Contains(search.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<FeedbackCategoryEnum>(category, true, out var parsedCategory))
                query = query.Where(f => f.FeedbackCategory == parsedCategory);

            if (from.HasValue)
                query = query.Where(f => f.CreatedTime.Date >= from.Value.ToDateTime(TimeOnly.MinValue));

            if (to.HasValue)
                query = query.Where(f => f.CreatedTime.Date <= to.Value.ToDateTime(TimeOnly.MaxValue));

            if (userId.HasValue)
                query = query.Where(f => f.UserId == userId.Value);

            if (tripId.HasValue)
                query = query.Where(f => f.TripId == tripId.Value);

            if (minRating.HasValue)
                query = query.Where(f => f.Rating >= minRating.Value);

            if (maxRating.HasValue)
                query = query.Where(f => f.Rating <= maxRating.Value);

            query = sortAsc
                ? query.OrderBy(f => f.CreatedTime)
                : query.OrderByDescending(f => f.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ResponseFeedbackModel>>(pagedItems);

            return new BasePaginatedList<ResponseFeedbackModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseFeedbackModel> GetByIdAsync(Guid feedbackId)
        {
            var feedback = await _unitOfWork.GetRepository<Feedback>().GetByIdAsync(feedbackId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá không tồn tại.");

            if (feedback.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá đã bị xóa.");

            return _mapper.Map<ResponseFeedbackModel>(feedback);
        }

        public async Task CreateAsync(CreateFeedbackModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            var trip = await _unitOfWork.GetRepository<Trip>().GetByIdAsync(model.TripId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi không tồn tại.");

            if (trip.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi đã bị xóa.");

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

        public async Task DeleteAsync(Guid feedbackId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var feedback = await _unitOfWork.GetRepository<Feedback>().GetByIdAsync(feedbackId)
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

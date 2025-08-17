using AutoMapper;
using AutoMapper.QueryableExtensions;
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
        private readonly IGenericRepository<Feedback> _feedbackRepo;
        private readonly IGenericRepository<Trip> _tripRepo;

        public FeedbackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _feedbackRepo = _unitOfWork.GetRepository<Feedback>();
            _tripRepo = _unitOfWork.GetRepository<Trip>();
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
            var query = _feedbackRepo
                .GetQueryable()
                .Include(f => f.User)
                .Include(f => f.Trip)
                .Where(f => !f.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(f => f.Message.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));

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

            var result = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ProjectTo<ResponseFeedbackModel>(_mapper.ConfigurationProvider)
                .AsNoTracking()
                .ToListAsync();

            return new BasePaginatedList<ResponseFeedbackModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseFeedbackModel> GetByIdAsync(Guid feedbackId)
        {
            var feedback = await _feedbackRepo.GetQueryable()
                .Where(f => f.Id == feedbackId && !f.DeletedTime.HasValue)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá không tồn tại.");

            return _mapper.Map<ResponseFeedbackModel>(feedback);
        }

        public async Task CreateAsync(CreateFeedbackModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            var trip = await _tripRepo.GetQueryable()
                .Where(t => t.Id == model.TripId && !t.DeletedTime.HasValue)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi không tồn tại.");

            if (trip.Status != TripStatusEnum.COMPLETED)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Chuyến đi này chưa kết thúc.");

            if (!trip.Attendances.Any(a => a.HistoryTicket.UserId == userIdGuid && a.Status == AttendanceStatusEnum.CHECKED_OUT))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Bạn chưa check-in cho chuyến đi này.");

            var hasExistingFeedback = await _feedbackRepo.GetQueryable()
                .AnyAsync(f => f.TripId == model.TripId &&
                          f.UserId == userIdGuid &&
                          !f.DeletedTime.HasValue);

            if (hasExistingFeedback)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Bạn đã đánh giá cho chuyến đi này.");

            if (!Enum.TryParse<FeedbackCategoryEnum>(model.FeedbackCategory, true, out var categoryEnum) || !Enum.IsDefined(typeof(FeedbackCategoryEnum), categoryEnum))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại đánh giá không hợp lệ.");

            if (string.IsNullOrWhiteSpace(model.Message))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền nội dung đánh giá.");

            if (model.Rating < 1 || model.Rating > 5)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Đánh giá phải nằm trong khoảng từ 1 đến 5.");

            var newFeedback = _mapper.Map<Feedback>(model);

            newFeedback.FeedbackCategory = categoryEnum;
            newFeedback.UserId = userIdGuid;
            newFeedback.CreatedBy = userId;
            newFeedback.LastUpdatedBy = userId;

            await _feedbackRepo.InsertAsync(newFeedback);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid feedbackId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var feedback = await _feedbackRepo.GetQueryable()
                .Where(f => f.Id == feedbackId && !f.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Đánh giá không tồn tại.");

            feedback.LastUpdatedTime = CoreHelper.SystemTimeNow;
            feedback.LastUpdatedBy = userId;
            feedback.DeletedTime = CoreHelper.SystemTimeNow;
            feedback.DeletedBy = userId;

            await _feedbackRepo.UpdateAsync(feedback);
            await _unitOfWork.SaveAsync();
        }
    }
}

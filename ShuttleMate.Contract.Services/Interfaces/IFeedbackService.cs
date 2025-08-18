using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.FeedbackModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task<BasePaginatedList<ResponseFeedbackModel>> GetAllAsync(
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
            int pageSize = 10);
        Task<ResponseFeedbackModel> GetByIdAsync(Guid feedbackId);
        Task CreateAsync(CreateFeedbackModel model);
        Task DeleteAsync(Guid feedbackId);
    }
}

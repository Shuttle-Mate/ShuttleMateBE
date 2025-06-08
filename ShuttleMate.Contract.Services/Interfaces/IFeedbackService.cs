using ShuttleMate.ModelViews.FeedbackModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task<IEnumerable<ResponseFeedbackModel>> GetAllAdminAsync();
        Task<IEnumerable<ResponseFeedbackModel>> GetAllMyAsync();
        Task<ResponseFeedbackModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateFeedbackModel model);
        Task DeleteAsync(Guid id);
    }
}

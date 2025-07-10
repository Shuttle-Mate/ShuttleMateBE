using ShuttleMate.ModelViews.ResponseSupportModelViews;
using ShuttleMate.ModelViews.SupportRequestModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ISupportRequestService
    {
        Task<IEnumerable<ResponseSupportRequestModel>> GetAllAsync();
        Task<IEnumerable<ResponseSupportRequestModel>> GetAllMyAsync();
        Task<IEnumerable<ResponseResponseSupportModel>> GetAllMyResponseAsync(Guid id);
        Task<ResponseSupportRequestModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateSupportRequestModel model);
        Task EscalateAsync(Guid id);
        Task ResolveAsync(Guid id);
        Task CancelAsync(Guid id);
    }
}

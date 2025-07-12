using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.ResponseSupportModelViews;
using ShuttleMate.ModelViews.SupportRequestModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ISupportRequestService
    {
        Task<BasePaginatedList<ResponseSupportRequestModel>> GetAllAsync(string? category, string? status, string? search, bool sortAsc = false, int page = 1, int pageSize = 10);
        Task<IEnumerable<ResponseSupportRequestModel>> GetAllMyAsync(string? category, string? status, string? search, bool sortAsc = false);
        Task<IEnumerable<ResponseResponseSupportModel>> GetAllResponsesAsync(Guid id);
        Task<ResponseSupportRequestModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateSupportRequestModel model);
        Task ResolveAsync(Guid id);
        Task CancelAsync(Guid id);
    }
}

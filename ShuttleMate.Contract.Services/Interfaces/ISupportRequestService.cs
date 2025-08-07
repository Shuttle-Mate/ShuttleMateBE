using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.ResponseSupportModelViews;
using ShuttleMate.ModelViews.SupportRequestModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ISupportRequestService
    {
        Task<BasePaginatedList<ResponseSupportRequestModel>> GetAllAsync(string? category, string? status, string? search, Guid? userId, bool sortAsc = false, int page = 1, int pageSize = 10);
        Task<IEnumerable<ResponseResponseSupportModel>> GetAllResponsesAsync(Guid supportRequestId);
        Task<ResponseSupportRequestModel> GetByIdAsync(Guid supportRequestId);
        Task CreateAsync(CreateSupportRequestModel model);
        Task UpdateStatusAsync(Guid supportRequestId, UpdateSupportRequestModel model);
    }
}

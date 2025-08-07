using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IStopService
    {
        Task<BasePaginatedList<ResponseStopModel>> GetAllAsync(string? search, Guid? wardId, bool sortAsc = false, int page = 0, int pageSize = 10);
        Task<ResponseStopModel> GetByIdAsync(Guid stopId);
        Task CreateAsync(CreateStopModel model);
        Task UpdateAsync(Guid stopId, UpdateStopModel model);
        Task DeleteAsync(Guid stopId);
    }
}

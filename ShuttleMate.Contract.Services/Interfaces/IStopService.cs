using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IStopService
    {
        Task<BasePaginatedList<ResponseStopModel>> GetAllAsync(string? search, Guid? wardId, bool sortAsc = false, int page = 0, int pageSize = 10);
        //Task<IEnumerable<ResponseSearchStopModel>> SearchAsync(string address);
        Task<ResponseStopModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateStopModel model);
        Task UpdateAsync(Guid id, UpdateStopModel model);
        Task DeleteAsync(Guid id);
    }
}

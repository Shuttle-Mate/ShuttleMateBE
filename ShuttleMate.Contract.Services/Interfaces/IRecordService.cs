using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.RecordModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IRecordService
    {
        Task<BasePaginatedList<ResponseRecordModel>> GetAllAsync(Guid? tripId, DateTime? from, DateTime? to, bool sortAsc = false, int page = 0, int pageSize = 10);
        Task<ResponseRecordModel> GetByIdAsync(Guid recordId);
        Task CreateAsync(CreateRecordModel model);
        Task UpdateAsync(Guid recordId, UpdateRecordModel model);
        Task DeleteAsync(Guid recordId);
    }
}

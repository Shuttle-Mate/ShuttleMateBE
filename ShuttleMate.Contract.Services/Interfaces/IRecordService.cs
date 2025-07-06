using ShuttleMate.ModelViews.RecordModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IRecordService
    {
        Task<IEnumerable<ResponseRecordModel>> GetAllAsync();
        Task<ResponseRecordModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateRecordModel model);
        Task UpdateAsync(Guid id, UpdateRecordModel model);
        Task DeleteAsync(Guid id);
    }
}

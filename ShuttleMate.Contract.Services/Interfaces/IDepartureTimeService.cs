using ShuttleMate.ModelViews.DepartureTimeModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IDepartureTimeService
    {
        Task<IEnumerable<ResponseDepartureTimeModel>> GetAllAsync();
        Task<ResponseDepartureTimeModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateDepartureTimeModel model);
        Task UpdateAsync(Guid id, UpdateDepartureTimeModel model);
        Task DeleteAsync(Guid id);
    }
}

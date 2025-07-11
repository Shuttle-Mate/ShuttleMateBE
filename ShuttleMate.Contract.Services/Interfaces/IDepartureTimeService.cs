using ShuttleMate.ModelViews.DepartureTimeModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IDepartureTimeService
    {
        Task CreateAsync(CreateDepartureTimeModel model);
        Task UpdateAsync(UpdateDepartureTimeModel model);
    }
}

using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IScheduleService
    {
        Task CreateAsync(CreateScheduleModel model);
        Task UpdateAsync(UpdateScheduleModel model);
    }
}

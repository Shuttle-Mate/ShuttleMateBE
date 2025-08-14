using ShuttleMate.ModelViews.ScheduleOverrideModelView;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IScheduleOverrideService
    {
        Task CreateAsync(CreateScheduleOverrideModel model);
        Task UpdateAsync(Guid scheduleOverrideId, UpdateScheduleOverrideModel model);
        Task DeleteAsync(Guid scheduleOverrideId);
    }
}

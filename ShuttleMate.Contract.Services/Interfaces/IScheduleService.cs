using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IScheduleService
    {
        Task<BasePaginatedList<ResponseScheduleModel>> GetAllByRouteIdAsync(Guid routeId, string? direction, bool sortAsc, int page = 0, int pageSize = 10);
        Task<IEnumerable<ResponseTodayScheduleForDriverModel>> GetAllTodayAsync();
        Task<ResponseScheduleModel> GetByIdAsync(Guid scheduleId);
        Task CreateAsync(CreateScheduleModel model);
        Task UpdateAsync(Guid scheduleId, UpdateScheduleModel model);
        Task DeleteAsync(Guid scheduleId);
    }
}

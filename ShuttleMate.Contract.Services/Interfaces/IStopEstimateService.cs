using ShuttleMate.Contract.Repositories.Entities;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IStopEstimateService
    {
        Task CreateAsync(List<Schedule> schedules, Guid routeId);
        Task UpdateAsync(List<Schedule> schedules, Guid routeId);
    }
}

using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.TripModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ITripService
    {
        Task<Guid> StartTrip(Guid scheduleId);
        Task EndTrip(Guid tripId, Guid routeId, Guid schoolShiftId);
        Task<BasePaginatedList<ResponseTripModel>> GetAllPaging(GetTripQuery req);
        Task<ResponseTripModel> GetByIdAsync(Guid tripId);
        Task UpdateAsync(Guid tripId, UpdateTripModel model);
        Task<BasePaginatedList<RouteShiftModels>> GetRouteShiftAsync(Guid userId);
        //Task<BasePaginatedList<TripHistoryModel>> GetDriverTripHistory(Guid driverId, DateOnly? from, DateOnly? to);
        //Task<BasePaginatedList<TripHistoryModel>> GetStudentTripHistory(Guid studentId, DateOnly? from, DateOnly? to);
        //Task<BasePaginatedList<TripHistoryModel>> GetParentTripHistory(Guid parentId, Guid studentId, DateOnly? from, DateOnly? to);
        Task<BasePaginatedList<TripHistoryModel>> GetDriverTripHistory(Guid driverId, DateOnly? from, DateOnly? to, int page = 0, int pageSize = 10);
        Task<BasePaginatedList<TripHistoryModel>> GetStudentTripHistory(Guid studentId, DateOnly? from, DateOnly? to, int page = 0, int pageSize = 10);
        Task<BasePaginatedList<TripHistoryModel>> GetParentTripHistory(Guid parentId, Guid studentId, DateOnly? from, DateOnly? to, int page = 0, int pageSize = 10);
    }
}

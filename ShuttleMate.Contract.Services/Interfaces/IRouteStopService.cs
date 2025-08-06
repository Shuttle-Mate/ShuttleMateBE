using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IRouteStopService
    {
        Task AssignStopsToRouteAsync(AssignStopsToRouteModel model);
        Task<BasePaginatedList<StopWithRouteResponseModel>> SearchStopWithRoutes(double lat, double lng, Guid schoolId, int page = 0, int pageSize = 10);
    }
}

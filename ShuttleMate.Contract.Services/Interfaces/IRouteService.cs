using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IRouteService
    {
        Task CreateRoute(RouteModel model);
        Task<BasePaginatedList<ResponseRouteModel>> GetAll(GetRouteQuery query);
        Task<BasePaginatedList<StopWithOrderModel>> StopListByRoute(GetRouteStopQuery req, Guid routeId);
        Task<ResponseRouteModel> GetById(Guid routeId);
        Task UpdateRoute(UpdateRouteModel model);
        Task DeleteRoute(Guid routeId);
        Task UpdateRouteInformationAsync(Guid routeId);
    }
}

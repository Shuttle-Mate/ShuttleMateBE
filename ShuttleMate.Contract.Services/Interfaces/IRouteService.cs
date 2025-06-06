using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.RouteModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IRouteService
    {
        Task CreateRoute(RouteModel model);
        Task<List<ResponseRouteModel>> GetAll();
        Task<ResponseRouteModel> GetById(Guid routeId);
        Task UpdateRoute(UpdateRouteModel model);
        Task DeleteRoute(Guid routeId);
    }
}

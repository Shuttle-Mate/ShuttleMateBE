using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IRouteStopService
    {
        Task AssignStopsToRouteAsync(AssignStopsToRouteModel model);
        Task<List<ResponseShuttleModel>> GetAll();

    }
}

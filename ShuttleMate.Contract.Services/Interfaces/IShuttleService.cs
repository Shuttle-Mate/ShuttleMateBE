using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.ShuttleModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IShuttleService
    {
        Task CreateShuttle(ShuttleModel model);
        Task<List<ResponseShuttleModel>> GetAll();
        Task<ResponseShuttleModel> GetById(Guid shuttleId);
        Task UpdateShuttle(UpdateShuttleModel model);
        Task DeleteShuttle(Guid shuttleId);
    }
}

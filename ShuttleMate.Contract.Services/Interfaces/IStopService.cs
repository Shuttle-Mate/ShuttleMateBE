using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IStopService
    {
        Task CreateStop(StopModel model);
        Task<List<ResponseStopModel>> GetAll();
        Task<ResponseStopModel> GetById(Guid stopId);
        Task UpdateStop(UpdateStopModel model);
        Task DeleteStop(Guid stopId);
    }
}

using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.ModelViews.TripModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ITripService
    {
        Task StartTrip(TripModel model);
        //Task<List<ResponseShuttleModel>> GetAll();
        Task<BasePaginatedList<ResponseTripModel>> GetAllPaging(GetTripQuery req);
        Task<ResponseTripModel> GetById(Guid tripId);
        Task UpdateTrip(UpdateTripModel model);
        //Task DeleteShuttle(Guid shuttleId);
    }
}

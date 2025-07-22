using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Pkix;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.TripModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class TripService : ITripService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public TripService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateTrip(TripModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var newTrip = _mapper.Map<Trip>(model);
            newTrip.CreatedBy = userId;
            newTrip.LastUpdatedBy = userId;
            newTrip.EndTime = null; // Assuming EndTime is nullable and not provided in the model
            await _unitOfWork.GetRepository<Trip>().InsertAsync(newTrip);
            await _unitOfWork.SaveAsync();
        }

        public Task<BasePaginatedList<ResponseTripModel>> GetAllPaging(GetTripQuery req)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseTripModel> GetById(Guid tripId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTrip(UpdateTripModel model)
        {
            throw new NotImplementedException();
        }
    }
}

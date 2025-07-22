using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.ModelViews.TripModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.MapperProfile
{
    public class TripProfile : Profile
    {
        public TripProfile()
        {
            CreateMap<Trip, TripModel>().ReverseMap();
            CreateMap<Trip, ResponseTripModel>().ReverseMap();
            CreateMap<Trip, UpdateTripModel>().ReverseMap();
        }
    }
}

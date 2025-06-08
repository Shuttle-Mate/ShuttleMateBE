using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ShuttleModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class ShuttleProfile : Profile
    {
        public ShuttleProfile() 
        {
            CreateMap<Shuttle, ShuttleModel>().ReverseMap();
            CreateMap<Shuttle, ResponseShuttleModel>().ReverseMap();
            CreateMap<Shuttle, UpdateShuttleModel>().ReverseMap();
        }
    }
}

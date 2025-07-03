using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class StopProfile : Profile
    {
        public StopProfile()
        {
            CreateMap<Stop, StopModel>().ReverseMap();
            CreateMap<Stop, ResponseStopModel>().ReverseMap();
            CreateMap<Stop, UpdateStopModel>().ReverseMap();
        }
    }
}

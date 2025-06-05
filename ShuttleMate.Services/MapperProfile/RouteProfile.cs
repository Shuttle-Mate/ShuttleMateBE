using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.RouteModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class RouteProfile : Profile
    {
        public RouteProfile()
        {
            CreateMap<Route, RouteModel>().ReverseMap();
            CreateMap<Route, ResponseRouteModel>().ReverseMap();
            CreateMap<Route, UpdateRouteModel>().ReverseMap();
        }

    }
}

using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.SchoolModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.MapperProfile
{
    public class SchoolProfile : Profile
    {
        public SchoolProfile()
        {
            CreateMap<School, CreateSchoolModel>().ReverseMap();
            CreateMap<School, SchoolResponseModel>().ReverseMap();
            CreateMap<School, UpdateSchoolModel>().ReverseMap();
        }
    }
}

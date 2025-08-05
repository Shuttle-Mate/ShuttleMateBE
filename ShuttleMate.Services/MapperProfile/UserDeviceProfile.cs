using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.TripModelViews;
using ShuttleMate.ModelViews.UserDeviceModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.MapperProfile
{
    public class UserDeviceProfile : Profile
    {
        public UserDeviceProfile()
        {
            CreateMap<UserDevice, UserDeviceModel>().ReverseMap();
        }
    }
}

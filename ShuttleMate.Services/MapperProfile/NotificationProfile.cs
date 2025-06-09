using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.NotificationModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotiModel>().ReverseMap();
            CreateMap<Notification, ResponseNotiModel>().ReverseMap();
            CreateMap<Notification, UpdateNotiModel>().ReverseMap();
        }
    }
}

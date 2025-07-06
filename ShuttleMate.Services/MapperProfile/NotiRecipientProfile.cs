using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.NotiRecipientModelView;

namespace ShuttleMate.Services.MapperProfile
{
    public class NotiRecipientProfile : Profile
    {
        public NotiRecipientProfile()
        {
            CreateMap<NotificationRecipient, NotiRecipientModel>().ReverseMap();
            CreateMap<NotificationRecipient, ResponseNotiRecipientModel>().ReverseMap();
            CreateMap<NotificationRecipient, UpdateNotiRecipientModel>().ReverseMap();
        }
    }
}

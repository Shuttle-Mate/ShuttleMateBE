using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.ModelViews.NotiTemplateModelView;
using ShuttleMate.ModelViews.PromotionModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.MapperProfile
{
    public class NotiTemplateProfile : Profile
    {
        public NotiTemplateProfile()
        {
            CreateMap<NotificationTemplate, ResponseNotiTemplateModel>().ReverseMap();
            CreateMap<NotificationTemplate, NotiTemplateModel>().ReverseMap();
            CreateMap<NotificationTemplate, UpdateNotiTemplateModel>().ReverseMap();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class AttendanceProfile : Profile
    {
        public AttendanceProfile()
        {
            CreateMap<Attendance, AttendanceModel>().ReverseMap();
            CreateMap<Attendance, ResponseAttendanceModel>().ReverseMap();
            CreateMap<Attendance, CheckInModel>().ReverseMap();
            CreateMap<Attendance, CheckOutModel>().ReverseMap();
            CreateMap<Attendance, UpdateAttendanceModel>().ReverseMap();
        }
    }
}

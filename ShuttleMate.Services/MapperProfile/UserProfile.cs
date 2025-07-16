using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, ListStudentInSchoolResponse>()
                .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent.FullName))
                .ReverseMap();

        }
    }
}

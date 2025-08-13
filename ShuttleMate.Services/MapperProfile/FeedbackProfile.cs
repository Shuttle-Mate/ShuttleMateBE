using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.FeedbackModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class FeedbackProfile : Profile
    {
        public FeedbackProfile()
        {
            CreateMap<Feedback, ResponseFeedbackModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FeedbackCategory, opt => opt.MapFrom(src => src.FeedbackCategory.ToString()))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.TripId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.CreatedTime))
                .ReverseMap();
            CreateMap<Feedback, CreateFeedbackModel>()
                .ForMember(dest => dest.FeedbackCategory, opt => opt.MapFrom(src => src.FeedbackCategory.ToString()))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.TripId))
                .ReverseMap();
        }
    }
}

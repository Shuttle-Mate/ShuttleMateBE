using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.FeedbackModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.MapperProfile
{
    public class FeedbackProfile : Profile
    {
        public FeedbackProfile()
        {
            CreateMap<Feedback, ResponseFeedbackModel>()
                .ForMember(dest => dest.FeedbackCategory, opt => opt.MapFrom(src => ConvertFeedbackCategoryToVietnameseString(src.FeedbackCategory)))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ReverseMap();
            CreateMap<Feedback, CreateFeedbackModel>()
                .ForMember(dest => dest.FeedbackCategory, opt => opt.MapFrom(src => src.FeedbackCategory))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ReverseMap();
        }

        private string ConvertFeedbackCategoryToVietnameseString(FeedbackCategoryEnum category)
        {
            return category switch
            {
                FeedbackCategoryEnum.SHUTTLE_OPERATION => "SHUTTLE_OPERATION",
                FeedbackCategoryEnum.APP_TECHNICAL => "APP_TECHNICAL",
                FeedbackCategoryEnum.OTHER => "OTHER",
                _ => "UNKNOWN"
            };
        }
    }
}

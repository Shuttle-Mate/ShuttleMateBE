using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.WithdrawalRequestModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.MapperProfile
{
    public class WithdrawalRequestProfile : Profile
    {
        public WithdrawalRequestProfile()
        {
            CreateMap<WithdrawalRequest, ResponseWithdrawalRequestModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.CreatedTime))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ConvertWithdrawalStatusToUppercaseString(src.Status)))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.BankAccount, opt => opt.MapFrom(src => src.BankAccount))
                .ForMember(dest => dest.BankAccountName, opt => opt.MapFrom(src => src.BankAccountName))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.BankName))
                .ForMember(dest => dest.RejectReason, opt => opt.MapFrom(src => src.RejectReason ?? string.Empty))
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.TransactionId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ReverseMap();
            CreateMap<WithdrawalRequest, CreateWithdrawalRequestModel>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.TransactionId))
                .ForMember(dest => dest.BankAccount, opt => opt.MapFrom(src => src.BankAccount))
                .ForMember(dest => dest.BankAccountName, opt => opt.MapFrom(src => src.BankAccountName))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.BankName))
                .ReverseMap();
            CreateMap<WithdrawalRequest, UpdateWithdrawalRequestModel>()
                .ForMember(dest => dest.BankAccount, opt => opt.MapFrom(src => src.BankAccount))
                .ForMember(dest => dest.BankAccountName, opt => opt.MapFrom(src => src.BankAccountName))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.BankName))
                .ReverseMap();
        }

        private string ConvertWithdrawalStatusToUppercaseString(WithdrawalRequestStatusEnum status)
        {
            return status switch
            {
                WithdrawalRequestStatusEnum.IN_PROGRESS => "IN_PROGRESS",
                WithdrawalRequestStatusEnum.COMPLETED => "COMPLETED",
                WithdrawalRequestStatusEnum.REJECTED => "REJECTED",
                _ => "UNKNOWN"
            };
        }
    }
}

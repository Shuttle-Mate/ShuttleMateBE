using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.RoleModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IAuthService
    {
        Task ForgotPassword(EmailModelView model);
        Task Register(RegisterModel model);
        Task ResendConfirmationEmail(EmailModelView emailModelView);
        Task<Guid> ConfirmEmail(ConfirmEmailModel model);
        Task<LoginResponse> LoginAsync(LoginRequestModel request);
        Task ResetPassword(ResetPasswordModelView model);
        Task<Guid> VerifyOtp(ConfirmOTPModelView model);
        Task<string> ChangePasswordFromForgetPassword(ChangePasswordFromForgetPasswordModel model);
        Task<LoginResponse> RefreshToken(RefreshTokenModel refreshTokenModel);
        Task LogoutAsync(RefreshTokenModel model);
        Task CreateUserAdmin(CreateUserAdminModel model);
    }
}

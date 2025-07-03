using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Core.Bases;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.RoleModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        /// <summary>
        /// True la Student, false la Parent
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            await _authService.Register(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đăng kí thành công!"
            ));
        }
        /// <summary>
        ///Rolename: 0 là Student, 1 là Parent, 2 là Operator, 3 là Driver
        /// </summary>
        [HttpPost("Create_User_Admin")]
        public async Task<IActionResult> CreateUserAdmin(CreateUserAdminModel model)
        {
            await _authService.CreateUserAdmin(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đăng kí thành công!"
            ));
        }

        [HttpPatch("confirm-otp-email")]
        public async Task<IActionResult> ConfirmOTPEmailVerification(ConfirmEmailModel model)
        {
            Guid res = await _authService.ConfirmEmail(model);
            return Ok(new BaseResponseModel<Guid>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        [HttpPatch("resend-confirm-email")]
        public async Task<IActionResult> ResendConfirmationEmail(EmailModelView model)
        {
            await _authService.ResendConfirmationEmail(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Gửi lại email thành công!"
            ));
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestModel request)
        {
            LoginResponse res = await _authService.LoginAsync(request);
            return Ok(new BaseResponseModel<LoginResponse>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenModel model)
        {
            LoginResponse? res = await _authService.RefreshToken(model);
            return Ok(new BaseResponseModel<LoginResponse>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(EmailModelView model)
        {
            await _authService.ForgotPassword(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đã gửi email xác nhận yêu cầu thay đổi mật khẩu."
            ));
        }

        [HttpPatch("forget-password/confirm-otp")]
        public async Task<IActionResult> ConfirmOTPResetPassword(ConfirmOTPModelView model)
        {
            Guid res = await _authService.VerifyOtp(model);
            return Ok(new BaseResponseModel<Guid>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        [HttpPatch("change-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordModelView model)
        {
            await _authService.ResetPassword(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đã đặt lại mật khẩu thành công!"
            ));
        }

        [HttpPatch("logout")]
        public async Task<IActionResult> Logout(RefreshTokenModel model)
        {
            await _authService.LogoutAsync(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đăng xuất thành công!"
            ));
        }

    }
}

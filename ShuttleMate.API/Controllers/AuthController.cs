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
        [HttpPost("Create_Role")]
        public async Task<IActionResult> CreateRole(RoleModel model)
        {
            await _authService.CreateRole(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Tạo vai trò thành công!"
            ));
        }


        [HttpPost("Register_User")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            await _authService.Register(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đăng kí thành công!"
            ));
        }

        [HttpPatch("Confirm_OTP_Email")]
        public async Task<IActionResult> ConfirmOTPEmailVerification(ConfirmEmailModel model)
        {
            string res = await _authService.ConfirmEmail(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        [HttpPatch("Resend_Confirmation_Email")]
        public async Task<IActionResult> ResendConfirmationEmail(EmailModelView model)
        {
            await _authService.ResendConfirmationEmail(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Gửi lại email thành công!"
            ));
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequestModel request)
        {
            LoginResponse res = await _authService.LoginAsync(request);
            return Ok(new BaseResponseModel<LoginResponse>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }
        [HttpPost("Check_Email")]
        public async Task<IActionResult> CheckEmail(ConfirmEmailModel model)
        {
            var res = await _authService.ConfirmEmail(model);
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(RefreshTokenModel model)
        {
            LoginResponse? res = await _authService.RefreshToken(model);
            return Ok(new BaseResponseModel<LoginResponse>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        [HttpPost("Forgot_Password")]
        public async Task<IActionResult> ForgotPassword(EmailModelView model)
        {
            await _authService.ForgotPassword(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đã gửi email xác nhận yêu cầu thay đổi mật khẩu."
            ));
        }

        [HttpPatch("Confirm_OTP_Forget_Password")]
        public async Task<IActionResult> ConfirmOTPResetPassword(ConfirmOTPModelView model)
        {
            string res = await _authService.VerifyOtp(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        [HttpPatch("Reset_Password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordModelView model)
        {
            await _authService.ResetPassword(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Đã đặt lại mật khẩu thành công!"
            ));
        }

    }
}

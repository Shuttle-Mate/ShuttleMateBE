using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.UserModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        /// <summary>
        /// Admin khóa tài khoản của người dùng
        /// </summary>
        [HttpPatch("Block-User-For-Admin")]
        public async Task<IActionResult> BlockUserForAdmin(BlockUserForAdminModel model)
        {
            var res = await _userService.BlockUserForAdmin(model);
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }
        /// <summary>
        /// Admin mở khóa tài khoản của người dùng
        /// </summary>
        [HttpPatch("UnBlock-User-For-Admin")]
        public async Task<IActionResult> UnBlockUserForAdmin(UnBlockUserForAdminModel model)
        {
            var res = await _userService.UnBlockUserForAdmin(model);
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }
        [HttpPost("assign_role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignUserRoleModel model)
        {
            await _userService.AssignUserToRoleAsync(model.UserId, model.RoleId);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Gán vai trò cho người dùng thành công!"
            ));
        }
        [HttpGet("Get_Infor")]
        public async Task<IActionResult> GetInfor()
        {
            UserInforModel res = await _userService.GetInfor();
            return Ok(new BaseResponseModel<UserInforModel>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }
    }
}

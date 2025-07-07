using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        /// <summary>
        ///Rolename: 0 là Student, 1 là Parent, 2 là Operator, 3 là Driver, 4 là School
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUserAdmin(CreateUserAdminModel model)
        {
            await _userService.CreateUserAdmin(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Đăng kí thành công!"
            ));
        }
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] Guid? roleId, [FromQuery] string? cityId, string? name, bool? gender)
        {
            var users = await _userService.GetAllAsync(roleId, name, gender);

            return Ok(new BaseResponseModel<IEnumerable<AdminResponseUserModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: users
            ));
        }
        /// <summary>
        /// Admin khóa tài khoản của người dùng
        /// </summary>
        [HttpDelete]
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
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignUserRoleModel model)
        {
            await _userService.AssignUserToRoleAsync(model.UserId, model.RoleId);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Gán vai trò cho người dùng thành công!"
            ));
        }
        /// <summary>
        /// Admin gán parent
        /// </summary>
        [HttpPost("assign-parent")]
        public async Task<IActionResult> AssignParent([FromBody] AssignParentModel model)
        {
            await _userService.AssignParent(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Gán vai trò cho phụ huynh thành công!"
            ));
        }
        /// <summary>
        /// Student gán parent
        /// </summary>
        [HttpPost("assign-parent/student")]
        public async Task<IActionResult> AssignParentForStudent([FromBody] AssignParentForStudentModel model)
        {
            await _userService.AssignParentForParent(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Gán vai trò cho phụ huynh thành công!"
            ));
        }
        [HttpDelete("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveUserRoleModel model)
        {
            await _userService.RemoveUserToRoleAsync(model.UserId);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa vai trò cho người dùng thành công!"
            ));
        }
        [HttpGet("Get-Infor")]
        public async Task<IActionResult> GetInfor()
        {
            UserInforModel res = await _userService.GetInfor();
            return Ok(new BaseResponseModel<UserInforModel>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateProfile(UpdateProfileModel model)
        {
            await _userService.UpdateProfiel(model);
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 message: "Cập nhật tài khoản thành công!"
             ));
        }
    }
}

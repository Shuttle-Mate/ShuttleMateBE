using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.UserModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        /// <summary>
        ///Rolename: STUDENT, PARENT, OPERATOR, DRIVER, SCHOOL
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

        /// <summary>
        /// Lấy tất cả người dùng(Admin)
        /// </summary>
        /// <param name="gender">true là nam, false là nữ (tuỳ chọn)</param>
        /// <param name="violate">true là bị khóa, false là không khóa (tuỳ chọn).</param>
        /// <param name="schoolId">lọc theo id trường (tuỳ chọn).</param>
        /// <param name="parentId">Lọc theo id phụ huynh.(tuỳ chọn).</param>
        /// <param name="roleName"> STUDENT, PARENT, OPERATOR, DRIVER, SCHOOL</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers(int page = 0, int pageSize = 10, string? name = null, bool? gender = null, string? roleName = null, bool? violate = null, string? email = null, string? phone = null, Guid? schoolId = null, Guid? parentId = null)
        {
            var users = await _userService.GetAllAsync(page, pageSize, name, gender, roleName, violate, email, phone, schoolId, parentId);

            return Ok(new BaseResponseModel<BasePaginatedList<AdminResponseUserModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: users
            ));
        }

        /// <summary>
        /// Lấy tất cả hs trong cùng 1 tuyến và ca học
        /// </summary>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        /// <param name="routeId">Lấy id tuyền(bắt buộc).</param>
        /// <param name="schoolShiftId">Lấy id ca học(bắt buộc).</param>
        [HttpGet("student-in-route-and-shift")]
        public async Task<IActionResult> GetStudentInRouteAndShift(int page = 0, int pageSize = 10, Guid? routeId = null, Guid? schoolShiftId = null)
        {
            var users = await _userService.GetStudentInRouteAndShift(page, pageSize, routeId, schoolShiftId);

            return Ok(new BaseResponseModel<BasePaginatedList<ResponseStudentInRouteAndShiftModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: users
            ));
        }
        /// <summary>
        /// Lấy tất cả con của phụ huynh(Parent)
        /// </summary>
        /// <param name="id">Id của phụ huynh</param>
        [HttpGet("get-child")]
        public async Task<IActionResult> GetYourChild(Guid id)
        {
            var users = await _userService.GetYourChild(id);

            return Ok(new BaseResponseModel<IEnumerable<ReponseYourChild>>(
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
        /// <summary>
        /// HS/PH cập cập ca học(Chỉ có thể cập nhật ca học từ 19h tối thứ 7 đến 17h chiều Chủ nhật hàng tuần)
        /// </summary>
        [HttpPatch("update-school-for-user")]
        public async Task<IActionResult> UpdateSchoolForUser(Guid? id = null, UpdateSchoolForUserModel? model = null)
        {
            await _userService.UpdateSchoolForUser(id, model);
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 message: "Cập nhật ca học thành công!"
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
        [HttpGet("get-infor")]
        public async Task<IActionResult> GetInfor()
        {
            UserInforModel res = await _userService.GetInfor();
            return Ok(new BaseResponseModel<UserInforModel>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetById(Guid userId)
        {
            UserInforModel res = await _userService.GetById(userId);
            return Ok(new BaseResponseModel<UserInforModel>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 data: res
             ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateProfile(Guid? userId = null, UpdateProfileModel model = null)
        {
            await _userService.UpdateProfiel(userId, model);
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 message: "Cập nhật tài khoản thành công!"
             ));
        }
        /// <summary>
        /// Xóa phụ huynh(Student)
        /// </summary>
        [HttpDelete("remove-parent")]
        public async Task<IActionResult> RemoveParent()
        {
            await _userService.RemoveParent();
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 message: "Xóa phụ huynh thành công!"
             ));
        }
        /// <summary>
        /// Xóa học sinh(Parent)
        /// </summary>
        [HttpDelete("remove-student")]
        public async Task<IActionResult> RemoveStudent(RemoveStudentModel model)
        {
            await _userService.RemoveStudent(model);
            return Ok(new BaseResponseModel<string>(
                 statusCode: StatusCodes.Status200OK,
                 code: ResponseCodeConstants.SUCCESS,
                 message: "Xóa học sinh thành công!"
             ));
        }
    }
}

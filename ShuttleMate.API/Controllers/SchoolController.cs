using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchoolController : ControllerBase
    {
        private readonly ISchoolService _schoolService;
        public SchoolController(ISchoolService schoolService)
        {
            _schoolService = schoolService;
        }

        /// <summary>
        /// Lấy danh sách trường học.(ADMIN)
        /// </summary>
        /// <param name="search">Tìm kiếm theo tên, địa chỉ, email hoặc sđt của trường.</param>
        /// <param name="isActive">lọc theo trường có kích hoạt hay không(True:có, false:ko)(tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllAsync(int page = 0, int pageSize = 10, string? search = null, bool? isActive = null, bool sortAsc = false)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<SchoolResponseModel>>(
            statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetAllAsync(page, pageSize, search, isActive, sortAsc)));
        }
        /// <summary>
        /// Lấy danh sách học sinh của trường.(ADMIN/OPERATOR/SCHOOL)
        /// </summary>
        /// <param name="search">Tìm kiếm theo tên, địa chỉ, email hoặc sđt của trường.</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        /// <param name="schoolShiftId">Lọc hs theo id của ca học.</param>
        /// <param name="schoolId">id của trường bạn muốn xem(Bắt buộc, nếu schoolId = null thì sẽ trả rỗng).</param>
        [HttpGet("{schoolId}/students")]
        public async Task<IActionResult> GetAllStudentInSchoolForAdmin(int page = 0, int pageSize = 10, string? search = null, bool sortAsc = false, Guid? schoolShiftId = null, Guid? schoolId = null)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ListStudentInSchoolResponse>>(
            statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetAllStudentInSchoolForAdmin(page, pageSize, search, sortAsc, schoolShiftId, schoolId)));
        }
        /// <summary>
        /// Lấy danh sách tuyến đãn đến trường bạn quản lí.
        /// </summary>
        /// <param name="search">Tìm kiếm theo code, tên, OutBound, InBound của tuyến.</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        /// <param name="schoolId">id của trường bạn muốn xem.</param>
        [HttpGet("{schoolId}/routes")]
        public async Task<IActionResult> GetAllRouteToSchool(int page = 0, int pageSize = 10, string? search = null, bool sortAsc = false, Guid? schoolId = null)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<RouteToSchoolResponseModel>>(
            statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetAllRouteToSchool(page, pageSize, search, sortAsc, schoolId)));
        }
        /// <summary>
        /// Lấy chi tiết trường.
        /// </summary>
        [HttpGet("{schoolId}")]
        public async Task<IActionResult> GetSchoolById(Guid schoolId)
        {
            return Ok(new BaseResponseModel<SchoolResponseModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetById(schoolId)));
        }
        /// <summary>
        /// Xem danh sách điểm danh của trường.
        /// </summary>
        [HttpGet("{schoolId}/attendances")]
        public async Task<IActionResult> GetAttendanceOfSchool(int page = 0, int pageSize = 10, DateOnly? date = null, Guid? schoolShiftId = null, Guid? schoolId = null, string? directionOfTravel = null, bool sortAsc = false)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<AttendanceOfSchoolResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetAttendanceOfSchool(page, pageSize, date, schoolShiftId, schoolId, directionOfTravel, sortAsc)));
        }
        /// <summary>
        /// Gửi mail cho trường nhắc nhở:Type(SCHOOL_SHIFT, SCHOOL_INFOR)
        /// </summary>
        [HttpPost("send-email")]
        public async Task<IActionResult> CreateSchool(SendEmailToSchoolModel model)
        {
            await _schoolService.SendEmailToSchool(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "gửi email cho trường thành công!"
            ));
        }

        /// <summary>
        /// Cập nhật một trường.(ADMIN)
        /// </summary>
        /// <param name="schoolId">id đối với role school lấy là schoolId, đối với role admin lấy id từ list school.</param>

        [HttpPatch("{schoolId}")]
        public async Task<IActionResult> UpdateSchool(Guid schoolId, UpdateSchoolModel model)
        {
            await _schoolService.UpdateSchool(schoolId, model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật trường thành công!"
            ));
        }

        /// <summary>
        /// Xóa một trường.(ADMIN)
        /// </summary>
        [HttpDelete("{schoolId}")]
        public async Task<IActionResult> DeleteSchool(Guid schoolId)
        {
            await _schoolService.DeleteSchool(schoolId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa trường thành công!"
            ));
        }
    }
}

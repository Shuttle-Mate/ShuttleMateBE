using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }
        /// <summary>
        /// Lấy danh sách điểm danh của người dùng hiện tại (có thể lọc theo ngày).
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyAttendance([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var res = await _attendanceService.GetMyAttendance(fromDate, toDate);

            return Ok(new BaseResponseModel<object>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        /// <summary>
        /// Checkin
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CheckIn(CheckInModel model)
        {
            await _attendanceService.CheckIn(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "CheckIn thành công!"
            ));
        }
        /// <summary>
        /// Checkout
        /// </summary>
        [HttpPatch]
        public async Task<IActionResult> CheckOut(CheckOutModel model)
        {
            await _attendanceService.CheckOut(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "CheckOut thành công"
            ));
        }
        /// <summary>
        /// Get All cho operator/admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllAttendance()
        {
            var res = await _attendanceService.GetAll();
            return Ok(new BaseResponseModel<List<ResponseAttendanceModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        /// <summary>
        /// Get by id
        /// </summary>
        [HttpGet("{attendanceId}")]
        public async Task<IActionResult> GetAttendanceById(Guid attendanceId)
        {
            var res = await _attendanceService.GetById(attendanceId);
            return Ok(new BaseResponseModel<ResponseAttendanceModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        /// <summary>
        /// Xóa/Ẩn Attendance
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteAttendance(Guid attendanceId)
        {
            await _attendanceService.DeleteAttendance(attendanceId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Xóa điểm danh thành công"
            ));
        }
    }
}

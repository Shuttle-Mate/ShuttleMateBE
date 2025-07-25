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
                message: "CheckIn thành công!"
            ));
        }
        /// <summary>
        /// Checkout từng người
        /// </summary>
        [HttpPatch]
        public async Task<IActionResult> CheckOut(CheckOutModel model)
        {
            await _attendanceService.CheckOut(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "CheckOut thành công"
            ));
        }
        /// <summary>
        /// Checkout tất cả chuyến đi này
        /// </summary>
        [HttpPatch("check-out/trip")]
        public async Task<IActionResult> CheckOutList(Guid tripId, string checkOutLocation, string? notes = null)
        {
            await _attendanceService.BulkCheckOutByTrip(tripId, checkOutLocation, notes);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "CheckOut thành công"
            ));
        }
        /// <summary>
        /// Get All cho operator/admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllAttendance([FromQuery]GetAttendanceQuery query)
        {
            var res = await _attendanceService.GetAll(query);
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseAttendanceModel>>(
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
                message: "Xóa điểm danh thành công"
            ));
        }
    }
}

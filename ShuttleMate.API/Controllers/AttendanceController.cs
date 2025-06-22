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

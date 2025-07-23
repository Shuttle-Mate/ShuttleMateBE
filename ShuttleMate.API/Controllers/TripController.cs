using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.ModelViews.TripModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;
        public TripController(ITripService tripService)
        {
            _tripService = tripService;
        }

        /// <summary>
        /// bắt đầu chuyến đi dành cho tài xế, trả về tripId để EndTrip
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartTrip(Guid scheduleId)
        {
            var tripId = await _tripService.StartTrip(scheduleId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Bắt đầu chuyến đi thành công!",
                data: tripId.ToString()
            ));
        }
        /// <summary>
        /// tài xế kết thúc chuyến đi
        /// </summary>
        [HttpPatch("end")]
        public async Task<IActionResult> CheckOut(Guid tripId)
        {
            await _tripService.EndTrip(tripId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Kết thúc chuyến đi thành công"
            ));
        }
    }
}

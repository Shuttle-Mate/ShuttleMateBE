using Hangfire;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.FeedbackModelViews;
using ShuttleMate.ModelViews.TripModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/trip")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly IBackgroundJobClient _backgroundJob;

        public TripController(ITripService tripService, IBackgroundJobClient backgroundJob)
        {
            _tripService = tripService;
            _backgroundJob = backgroundJob;
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
        /// get all trip với filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTrip([FromQuery] GetTripQuery query)
        {
            var res = await _tripService.GetAllPaging(query);
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseTripModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        /// <summary>
        /// Lấy chi tiết chuyến đi.
        /// </summary>
        [HttpGet("{tripId}")]
        public async Task<IActionResult> GetTripById(Guid tripId)
        {
            return Ok(new BaseResponseModel<ResponseTripModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _tripService.GetByIdAsync(tripId)));
        }

        /// <summary>
        /// tài xế kết thúc chuyến đi
        /// </summary>
        [HttpPatch("end")]
        public async Task<IActionResult> EndTrip(Guid tripId, Guid routeId, Guid schoolShiftId)
        {
            await _tripService.EndTrip(tripId, routeId, schoolShiftId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Kết thúc chuyến đi thành công"
            ));
        }

        /// <summary>
        /// Cập nhật vị trí chuyến đi
        /// </summary>
        [HttpPatch("{tripId}")]
        public async Task<IActionResult> UpdateTripLocation(Guid tripId, UpdateTripModel model)
        {
            await _tripService.UpdateAsync(tripId, model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật vị trí chuyến đi thành công"
            ));
        }
    }
}

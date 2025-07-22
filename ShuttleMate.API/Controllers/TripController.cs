using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.ModelViews.TripModelViews;

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

        [HttpPost("start")]
        public async Task<IActionResult> StartTrip(TripModel model)
        {
            await _tripService.StartTrip(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Bắt đầu chuyến đi thành công!"
            ));
        }
    }
}

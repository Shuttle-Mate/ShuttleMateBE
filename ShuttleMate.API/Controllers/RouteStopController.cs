using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteStopController : ControllerBase
    {
        private readonly IRouteStopService _routeStopService;

        public RouteStopController(IRouteStopService routeStopService)
        {
            _routeStopService = routeStopService;
        }

        [HttpPost]
        public async Task<IActionResult> AssignStopToRoute(AssignStopsToRouteModel model)
        {
            await _routeStopService.AssignStopsToRouteAsync(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Gắn trạm dừng vào tuyến thành công!"
            ));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/route-stop")]
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

        [HttpGet]
        public async Task<IActionResult> GetStopWithRoute([FromQuery] GetRouteStopQuery req)
        {
            var res = await _routeStopService.SearchStopWithRoutes(req);
            return Ok(new BaseResponseModel<BasePaginatedList<StopWithRouteResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
    }
}

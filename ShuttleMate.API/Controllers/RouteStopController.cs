using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
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

        /// <summary>
        /// Lấy các trạm dừng gần nhất và tuyến liên quan.
        /// </summary>
        /// <param name="lat">Vĩ độ của địa điểm bắt đầu (bắt buộc).</param>
        /// <param name="lng">Kinh độ của địa điểm bắt đầu (bắt buộc).</param>
        /// <param name="schoolId">Id của trường (bắt buộc).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetStopWithRoute(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] Guid schoolId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10
        )
        {
            return Ok(new BaseResponseModel<BasePaginatedList<StopWithRouteResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _routeStopService.SearchStopWithRoutes(lat, lng, schoolId, page, pageSize)
            ));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.ScheduleModelViews;
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;
        private readonly IScheduleService _scheduleService;

        public RouteController(IRouteService routeService, IScheduleService scheduleService)
        {
            _routeService = routeService;
            _scheduleService = scheduleService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoute(RouteModel model)
        {
            await _routeService.CreateRoute(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo tuyến thành công!"
            ));
        }
        [HttpGet]
        public async Task<IActionResult> GetAllRoute([FromQuery] GetRouteQuery query)
        {
            var res = await _routeService.GetAll(query);
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseRouteModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{routeId}/stops")]
        public async Task<IActionResult> StopListByRoute([FromQuery] GetRouteStopQuery query, Guid routeId)
        {
            var res = await _routeService.StopListByRoute(query, routeId);
            return Ok(new BaseResponseModel<BasePaginatedList<StopWithOrderModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{routeId}")]
        public async Task<IActionResult> GetRouteById(Guid routeId)
        {
            var res = await _routeService.GetById(routeId);
            return Ok(new BaseResponseModel<ResponseRouteModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }

        /// <summary>
        /// Lấy danh sách lịch trình theo tuyến.
        /// </summary>
        /// <param name="routeId">Id của tuyến (bắt buộc).</param>
        /// <param name="direction">Hướng của tuyến: IN_BOUND, OUT_BOUND (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp giảm dần theo giờ khởi hành (true, mặc định) hoặc giảm dần (false).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpGet("{routeId}/schedules")]
        public async Task<IActionResult> GetSchedulesByRouteId(
        [FromRoute] Guid routeId,
        [FromQuery] string? direction,
        [FromQuery] bool sortAsc = true,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseScheduleModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _scheduleService.GetAllByRouteIdAsync(routeId, direction, sortAsc, page, pageSize)));
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateRoute(UpdateRouteModel model)
        {
            await _routeService.UpdateRoute(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật tuyến thành công"
            ));
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteRoute(Guid routeId)
        {
            await _routeService.DeleteRoute(routeId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa tuyến thành công"
            ));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;
        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoute(RouteModel model)
        {
            await _routeService.CreateRoute(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Tạo tuyết thành công!"
            ));
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await _routeService.GetAll();
            return Ok(new BaseResponseModel<List<ResponseRouteModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateRole(UpdateRouteModel model)
        {
            await _routeService.UpdateRoute(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Cập nhật tuyến thành công"
            ));
        }
    }
}

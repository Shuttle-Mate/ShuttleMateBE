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
    public class StopController : ControllerBase
    {
        private readonly IStopService _stopService;

        public StopController (IStopService stopService)
        {
            _stopService = stopService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateStop(StopModel model)
        {
            await _stopService.CreateStop(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo trạm dừng thành công!"
            ));
        }
        [HttpGet]
        public async Task<IActionResult> GetAllStops()
        {
            var res = await _stopService.GetAll();
            return Ok(new BaseResponseModel<List<ResponseStopModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{stopId}")]
        public async Task<IActionResult> GetStopById(Guid stopId)
        {
            var res = await _stopService.GetById(stopId);
            return Ok(new BaseResponseModel<ResponseStopModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateStop(UpdateStopModel model)
        {
            await _stopService.UpdateStop(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật trạm dừng thành công"
            ));
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteStop(Guid stopId)
        {
            await _stopService.DeleteStop(stopId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa trạm dừng thành công"
            ));
        }
    }
}

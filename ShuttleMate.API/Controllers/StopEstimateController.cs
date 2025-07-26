using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.StopEstimateModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/stop-estimate")]
    [ApiController]
    public class StopEstimateController : ControllerBase
    {
        private readonly IStopEstimateService _stopEstimateService;

        public StopEstimateController(IStopEstimateService stopEstimateService)
        {
            _stopEstimateService = stopEstimateService;
        }

        /// <summary>
        /// Lấy tất cả thời gian ước tính cho các điểm dừng.
        /// </summary>
        //[HttpGet]
        //public async Task<IActionResult> GetAllStopEstimates()
        //{
        //    var result = await _stopEstimateService.GetAllAsync();
        //    return Ok(new BaseResponseModel<IEnumerable<ResponseStopEstimateModel>>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: result));
        //}

        /// <summary>
        /// Lấy thời gian ước tính cho các điểm dừng của một tuyến xe.
        /// </summary>
        /// <param name="routeId">ID của tuyến xe cần lấy thời gian ước tính</param>
        [HttpGet("route/{routeId}")]
        public async Task<IActionResult> GetByRouteId(Guid routeId)
        {
            var result = await _stopEstimateService.GetByRouteIdAsync(routeId);
            return Ok(new BaseResponseModel<IEnumerable<ResponseStopEstimateModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result));
        }

        /// <summary>
        /// Tạo và tính toán các thời gian ước tính cho các điểm dừng của một tuyến xe.
        /// </summary>
        /// <param name="routeId">ID của tuyến xe cần tính toán thời gian ước tính cho các điểm dừng</param>
        [HttpPost("{routeId}")]
        public async Task<IActionResult> CreateStopEstimates(Guid routeId)
        {
            await _stopEstimateService.CreateAsync(routeId);

            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tính toán thời gian ước tính cho các điểm dừng của tuyến xe thành công."));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/stop")]
    [ApiController]
    public class StopController : ControllerBase
    {
        private readonly IStopService _stopService;

        public StopController(IStopService stopService)
        {
            _stopService = stopService;
        }

        /// <summary>
        /// Lấy danh sách trạm dừng.
        /// </summary>
        /// <param name="search">Tìm kiếm theo tên hoặc địa chỉ của trạm.</param>
        /// <param name="wardId">Lọc theo ID của phường (Guid, tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllStops(
        [FromQuery] string? search,
        [FromQuery] Guid? wardId,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseStopModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _stopService.GetAllAsync(search, wardId, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy chi tiết trạm dừng.
        /// </summary>
        [HttpGet("{stopId}")]
        public async Task<IActionResult> GetStopById(Guid stopId)
        {
            return Ok(new BaseResponseModel<ResponseStopModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _stopService.GetByIdAsync(stopId)));
        }

        /// <summary>
        /// Tạo một trạm dừng mới.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateStop(CreateStopModel model)
        {
            await _stopService.CreateAsync(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo trạm dừng thành công."
            ));
        }

        /// <summary>
        /// Cập nhật một trạm dừng.
        /// </summary>
        [HttpPatch("{stopId}")]
        public async Task<IActionResult> UpdateStop(Guid stopId, UpdateStopModel model)
        {
            await _stopService.UpdateAsync(stopId, model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật trạm dừng thành công."
            ));
        }

        /// <summary>
        /// Xóa một trạm dừng.
        /// </summary>
        [HttpDelete("{stopId}")]
        public async Task<IActionResult> DeleteStop(Guid stopId)
        {
            await _stopService.DeleteAsync(stopId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa trạm dừng thành công."
            ));
        }
    }
}

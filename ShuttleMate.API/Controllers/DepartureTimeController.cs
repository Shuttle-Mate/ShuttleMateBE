using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.DepartureTimeModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartureTimeController : ControllerBase
    {
        private readonly IDepartureTimeService _departureTimeService;

        public DepartureTimeController(IDepartureTimeService departureTimeService)
        {
            _departureTimeService = departureTimeService;
        }

        /// <summary>
        /// Lấy toàn bộ giờ bắt đầu.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDepartureTimes()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseDepartureTimeModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _departureTimeService.GetAllAsync()));
        }

        /// <summary>
        /// Lấy thời gian khởi hành bằng id.
        /// </summary>
        /// <param name="id">ID của thời gian khởi hành cần lấy</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartureTimeById(Guid id)
        {
            return Ok(new BaseResponseModel<ResponseDepartureTimeModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _departureTimeService.GetByIdAsync(id)));
        }

        /// <summary>
        /// Tạo một thời gian khởi hành mới.
        /// </summary>
        /// <param name="model">Thông tin thời gian khởi hành cần tạo</param>
        [HttpPost]
        public async Task<IActionResult> CreateDepartureTime(CreateDepartureTimeModel model)
        {
            await _departureTimeService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới thời gian khởi hành thành công."));
        }

        /// <summary>
        /// Cập nhật một thời gian khởi hành.
        /// </summary>
        /// <param name="id">ID của thời gian khởi hành cần cập nhật</param>
        /// <param name="model">Thông tin cập nhật cho thời gian khởi hành</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartureTime(Guid id, UpdateDepartureTimeModel model)
        {
            await _departureTimeService.UpdateAsync(id, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật thời gian khởi hành thành công."));
        }

        /// <summary>
        /// Xóa một thời gian khởi hành.
        /// </summary>
        /// <param name="id">id của thời gian khởi hành cần xóa.</param>
        ///
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartureTime(Guid id)
        {
            await _departureTimeService.DeleteAsync(id);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa thời gian khởi hành công."));
        }
    }
}

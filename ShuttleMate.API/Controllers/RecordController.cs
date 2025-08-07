using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RecordModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/record")]
    [ApiController]
    public class RecordController : ControllerBase
    {
        private readonly IRecordService _recordService;

        public RecordController(IRecordService recordService)
        {
            _recordService = recordService;
        }

        /// <summary>
        /// Lấy toàn bộ bản ghi vị trí.
        /// </summary>
        /// <param name="tripId">ID chuyến đi (tùy chọn).</param>
        /// <param name="from">Lọc từ dấu thời gian (tùy chọn).</param>
        /// <param name="to">Lọc đến dấu thời gian (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo dấu thời gian (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllRecords(
        [FromQuery] Guid? tripId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseRecordModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _recordService.GetAllAsync(tripId, from, to, sortAsc, page, pageSize)
            ));
        }

        /// <summary>
        /// Lấy chi tiết bản ghi vị trí.
        /// </summary>
        [HttpGet("{recordId}")]
        public async Task<IActionResult> GetRecordById(Guid recordId)
        {
            return Ok(new BaseResponseModel<ResponseRecordModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _recordService.GetByIdAsync(recordId)));
        }

        /// <summary>
        /// Tạo một bản ghi vị trí mới.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRecord(CreateRecordModel model)
        {
            await _recordService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới bản ghi vị trí thành công."));
        }

        /// <summary>
        /// Cập nhật một bản ghi vị trí.
        /// </summary>
        [HttpPut("{recordId}")]
        public async Task<IActionResult> UpdateRecord(Guid recordId, UpdateRecordModel model)
        {
            await _recordService.UpdateAsync(recordId, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật bản ghi vị trí thành công."));
        }

        /// <summary>
        /// Xóa một bản ghi vị trí.
        /// </summary>
        [HttpDelete("{recordId}")]
        public async Task<IActionResult> DeleteRecord(Guid recordId)
        {
            await _recordService.DeleteAsync(recordId);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa bản ghi vị trí thành công."));
        }
    }
}

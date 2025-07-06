using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RecordModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
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
        [HttpGet]
        public async Task<IActionResult> GetAllRecordsAdmin()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseRecordModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _recordService.GetAllAsync()));
        }

        /// <summary>
        /// Lấy bản ghi vị trí bằng id.
        /// </summary>
        /// <param name="id">ID của bản ghi vị trí cần lấy</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecordById(Guid id)
        {
            return Ok(new BaseResponseModel<ResponseRecordModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _recordService.GetByIdAsync(id)));
        }

        /// <summary>
        /// Tạo một bản ghi vị trí mới.
        /// </summary>
        /// <param name="model">Thông tin bản ghi vị trí cần tạo</param>
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
        /// Cập nhật trạng thái một bản ghi vị trí.
        /// </summary>
        /// <param name="id">ID của bản ghi vị trí cần cập nhật trạng thái</param>
        /// <param name="model">Thông tin cập nhật cho bản ghi vị trí</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecord(Guid id, UpdateRecordModel model)
        {
            await _recordService.UpdateAsync(id, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật bản ghi vị trí thành công."));
        }

        /// <summary>
        /// Xóa một bản ghi vị trí.
        /// </summary>
        /// <param name="id">id của bản ghi vị trí cần xóa.</param>
        ///
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecord(Guid id)
        {
            await _recordService.DeleteAsync(id);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa bản ghi vị trí công."));
        }
    }
}

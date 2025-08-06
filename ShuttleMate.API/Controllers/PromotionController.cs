using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.PromotionModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/promotion")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        /// <summary>
        /// Lấy toàn bộ khuyến mãi.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên khuyến mãi (tùy chọn).</param>
        /// <param name="type">Loại khuyến mãi: DIRECT_DISCOUNT, PERCENTAGE_DISCOUNT, FIXED_AMOUNT_DISCOUNT (tùy chọn).</param>
        /// <param name="isExpired">Trạng thái hết hạn: true (đã hết hạn), false (còn hiệu lực), null (tất cả) (tùy chọn).</param>
        /// <param name="startEndDate">Lọc từ ngày kết thúc (>=) (tùy chọn).</param>
        /// <param name="endEndDate">Lọc đến ngày kết thúc (<=) (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] bool? isExpired,
        [FromQuery] DateTime? startEndDate,
        [FromQuery] DateTime? endEndDate,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllAsync(search, type, isExpired, startEndDate, endEndDate, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy toàn bộ khuyến mãi của tôi (có tìm kiếm, lọc, phân trang, sắp xếp).
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên khuyến mãi (tùy chọn).</param>
        /// <param name="type">Loại khuyến mãi: DIRECT_DISCOUNT, PERCENTAGE_DISCOUNT, FIXED_AMOUNT_DISCOUNT (tùy chọn).</param>
        /// <param name="isExpired">Trạng thái hết hạn: true (đã hết hạn), false (còn hiệu lực), null (tất cả) (tùy chọn).</param>
        /// <param name="startEndDate">Lọc từ ngày kết thúc (>=) (tùy chọn).</param>
        /// <param name="endEndDate">Lọc đến ngày kết thúc (<=) (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllPromotionsMy(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] bool? isExpired,
        [FromQuery] DateTime? startEndDate,
        [FromQuery] DateTime? endEndDate,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllMyAsync(search, type, isExpired, startEndDate, endEndDate, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy toàn bộ khuyến mãi chưa lưu.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpGet("unsaved")]
        public async Task<IActionResult> GetAllUnsavedPromotions()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllUnsavedAsync()));
        }

        /// <summary>
        /// Lấy toàn bộ người dùng lưu một khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpGet("{id}/users")]
        public async Task<IActionResult> GetAllUsersSavedPromotion(Guid id)
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseUserPromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllUsersSavedAsync(id)));
        }

        /// <summary>
        /// Lấy chi tiết khuyến mãi.
        /// </summary>
        ////[Authorize(Roles = "Admin", "Student", "Parent")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotionById(Guid id)
        {
            return Ok(new BaseResponseModel<ResponsePromotionModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetByIdAsync(id)));
        }

        /// <summary>
        /// Tạo một khuyến mãi mới.
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePromotion(CreatePromotionModel model)
        {
            await _promotionService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới khuyến mãi thành công."));
        }

        /// <summary>
        /// Cập nhật một khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(Guid id, UpdatePromotionModel model)
        {
            await _promotionService.UpdateAsync(id, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật khuyến mãi thành công."));
        }

        /// <summary>
        /// Xóa một khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(Guid id)
        {
            await _promotionService.DeleteAsync(id);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa khuyến mãi công."));
        }

        /// <summary>
        /// Lưu một khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpPost("{id}/save")]
        public async Task<IActionResult> SavePromotion(Guid id)
        {
            await _promotionService.SavePromotionAsync(id);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Lưu khuyến mãi thành công."));
        }
    }
}

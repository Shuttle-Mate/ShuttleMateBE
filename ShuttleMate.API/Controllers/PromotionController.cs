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
        /// <param name="startEndDate">Lọc từ thời gian hết hạn (tùy chọn).</param>
        /// <param name="endEndDate">Lọc đến thời gian hết hạn (tùy chọn).</param>
        /// <param name="userId">ID người dùng (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo thời gian hết hạn (true, mặc định) hoặc giảm dần (false).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] bool? isExpired,
        [FromQuery] DateTime? startEndDate,
        [FromQuery] DateTime? endEndDate,
        [FromQuery] Guid? userId,
        [FromQuery] bool sortAsc = true,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllAsync(search, type, isExpired, startEndDate, endEndDate, userId, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy toàn bộ khuyến mãi đã lưu của người dùng có thể áp dụng khi mua vé.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpGet("applicable")]
        public async Task<IActionResult> GetAllApplicablePromotions(
        [FromQuery] Guid ticketId)
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllApplicableAsync(ticketId)));
        }

        ///// <summary>
        ///// Lấy toàn bộ khuyến mãi chưa lưu.
        ///// </summary>
        ////[Authorize(Roles = "Student", "Parent")]
        //[HttpGet("unsaved")]
        //public async Task<IActionResult> GetAllUnsavedPromotions()
        //{
        //    return Ok(new BaseResponseModel<IEnumerable<ResponsePromotionModel>>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: await _promotionService.GetAllUnsavedAsync()));
        //}

        ///// <summary>
        ///// Lấy toàn bộ người dùng lưu một khuyến mãi.
        ///// </summary>
        ////[Authorize(Roles = "Admin")]
        //[HttpGet("{promotionId}/users")]
        //public async Task<IActionResult> GetAllUsersSavedPromotion(Guid promotionId)
        //{
        //    return Ok(new BaseResponseModel<IEnumerable<ResponseUserPromotionModel>>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: await _promotionService.GetAllUsersSavedAsync(promotionId)));
        //}

        /// <summary>
        /// Lấy chi tiết khuyến mãi.
        /// </summary>
        ////[Authorize(Roles = "Admin", "Student", "Parent")]
        [HttpGet("{promotionId}")]
        public async Task<IActionResult> GetPromotionById(Guid promotionId)
        {
            return Ok(new BaseResponseModel<ResponsePromotionModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetByIdAsync(promotionId)));
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
        [HttpPut("{promotionId}")]
        public async Task<IActionResult> UpdatePromotion(Guid promotionId, UpdatePromotionModel model)
        {
            await _promotionService.UpdateAsync(promotionId, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật khuyến mãi thành công."));
        }

        /// <summary>
        /// Xóa một khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpDelete("{promotionId}")]
        public async Task<IActionResult> DeletePromotion(Guid promotionId)
        {
            await _promotionService.DeleteAsync(promotionId);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa khuyến mãi thành công."));
        }
    }
}

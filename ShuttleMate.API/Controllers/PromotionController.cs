using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.PromotionModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        /// <summary>
        /// Lấy toàn bộ khuyến mãi (Admin).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllPromotionsAdmin()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllAdminAsync()));
        }

        /// <summary>
        /// Lấy toàn bộ khuyến mãi của tôi.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllPromotionsMy()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllMyAsync()));
        }

        /// <summary>
        /// Lấy khuyến mãi bằng id.
        /// </summary>
        /// <param name="id">ID của khuyến mãi cần lấy</param>
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
        /// <param name="model">Thông tin khuyến mãi cần tạo</param>
        [HttpPost]
        public async Task<IActionResult> CreatePromotion(CreateDiscountPricePromotionModel model)
        {
            await _promotionService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới khuyến mãi thành công."));
        }

        /// <summary>
        /// Cập nhật trạng thái một khuyến mãi.
        /// </summary>
        /// <param name="id">ID của khuyến mãi cần cập nhật trạng thái</param>
        /// <param name="model">Thông tin cập nhật cho khuyến mãi</param>
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
        /// <param name="id">id của khuyến mãi cần xóa.</param>
        ///
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(Guid id)
        {
            await _promotionService.DeleteAsync(id);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa khuyến mãi công."));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
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
        /// Lấy toàn bộ khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllAsync()));
        }

        /// <summary>
        /// Lấy toàn bộ khuyến mãi của tôi.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpGet("my")]
        public async Task<IActionResult> GetAllPromotionsMy()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponsePromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllMyAsync()));
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
        /// Lấy toàn bộ người dùng của một khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Admin")]
        [HttpGet("{promotionId}/users")]
        public async Task<IActionResult> GetAllUsersSavedPromotion(Guid promotionId)
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseUserPromotionModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _promotionService.GetAllUsersSavedAsync(promotionId)));
        }

        /// <summary>
        /// Lấy khuyến mãi bằng id.
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
                message: "Xóa khuyến mãi công."));
        }

        /// <summary>
        /// Lưu một khuyến mãi.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpPost("{promotionId}/save")]
        public async Task<IActionResult> SavePromotion(Guid promotionId)
        {
            await _promotionService.SavePromotionAsync(promotionId);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Lưu khuyến mãi thành công."));
        }
    }
}

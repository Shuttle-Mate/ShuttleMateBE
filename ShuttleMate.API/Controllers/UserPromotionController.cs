using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.PromotionModelViews;
using ShuttleMate.ModelViews.UserPromotionModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/user-promotion")]
    [ApiController]
    public class UserPromotionController : ControllerBase
    {
        private readonly IUserPromotionService _userPromotionService;

        public UserPromotionController(IUserPromotionService userPromotionService)
        {
            _userPromotionService = userPromotionService;
        }

        /// <summary>
        /// Lưu một khuyến mãi.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SavePromotion(CreateUserPromotionModel model)
        {
            await _userPromotionService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Lưu khuyến mãi thành công."));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.WardModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/ward")]
    [ApiController]
    public class WardController : ControllerBase
    {
        private readonly IWardService _wardService;

        public WardController(IWardService wardService)
        {
            _wardService = wardService;
        }

        /// <summary>
        /// Lấy toàn bộ phường.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllWards()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseWardModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _wardService.GetAllAsync()));
        }
    }
}

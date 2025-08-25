using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ResponseSupportModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/response-support")]
    [ApiController]
    public class ResponseSupportController : ControllerBase
    {
        private readonly IResponseSupportService _responseSupportService;

        public ResponseSupportController(IResponseSupportService responseSupportService)
        {
            _responseSupportService = responseSupportService;
        }

        /// <summary>
        /// Tạo một phản hồi hỗ trợ mới.
        /// </summary>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpPost]
        public async Task<IActionResult> CreateResponseSupport(CreateResponseSupportModel model)
        {
            await _responseSupportService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới phản hồi hỗ trợ thành công."));
        }

        /// <summary>
        /// Xóa một phản hồi hỗ trợ.
        /// </summary>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpDelete("{responseSupportId}")]
        public async Task<IActionResult> DeleteResponseSupport(Guid responseSupportId)
        {
            await _responseSupportService.DeleteAsync(responseSupportId);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa phản hồi hỗ trợ thành công."));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.SupportRequestModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportRequestController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;

        public SupportRequestController(ISupportRequestService supportRequestService)
        {
            _supportRequestService = supportRequestService;
        }

        /// <summary>
        /// Lấy toàn bộ yêu cầu hỗ trợ (Admin).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSupportRequestsAdmin()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseSupportRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllAdminAsync()));
        }

        /// <summary>
        /// Lấy toàn bộ yêu cầu hỗ trợ của tôi.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllSupportRequestsMy()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseSupportRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllMyAsync()));
        }

        /// <summary>
        /// Lấy yêu cầu hỗ trợ bằng id.
        /// </summary>
        /// <param name="id">ID của yêu cầu hỗ trợ cần lấy</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupportRequestById(Guid id)
        {
            return Ok(new BaseResponseModel<ResponseSupportRequestModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetByIdAsync(id)));
        }

        /// <summary>
        /// Tạo một yêu cầu hỗ trợ mới.
        /// </summary>
        /// <param name="model">Thông tin yêu cầu hỗ trợ cần tạo</param>
        [HttpPost]
        public async Task<IActionResult> CreateSupportRequest(CreateSupportRequestModel model)
        {
            await _supportRequestService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới yêu cầu hỗ trợ thành công."));
        }

        /// <summary>
        /// Cập nhật trạng thái một yêu cầu hỗ trợ.
        /// </summary>
        /// <param name="id">ID của yêu cầu hỗ trợ cần cập nhật trạng thái</param>
        /// <param name="model">Thông tin cập nhật cho yêu cầu hỗ trợ</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> ChangeSupportRequestStatus(Guid id, UpdateSupportRequestModel model)
        {
            await _supportRequestService.ChangeStatusAsync(id, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật trạng thái yêu cầu hỗ trợ thành công."));
        }

        /// <summary>
        /// Xóa một yêu cầu hỗ trợ.
        /// </summary>
        /// <param name="id">id của yêu cầu hỗ trợ cần xóa.</param>
        ///
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupportRequest(Guid id)
        {
            await _supportRequestService.DeleteAsync(id);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa yêu cầu hỗ trợ công."));
        }
    }
}

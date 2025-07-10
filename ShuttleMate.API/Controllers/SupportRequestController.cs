using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ResponseSupportModelViews;
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
        /// Lấy toàn bộ yêu cầu hỗ trợ.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSupportRequests()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseSupportRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllAsync()));
        }

        /// <summary>
        /// Lấy toàn bộ yêu cầu hỗ trợ của tôi.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllMySupportRequests()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseSupportRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllMyAsync()));
        }

        /// <summary>
        /// Lấy toàn bộ phản hồi hỗ trợ của một yêu cầu hỗ trợ.
        /// </summary>
        [HttpGet("{supportRequestId}/responses")]
        public async Task<IActionResult> GetAllMySupportRequests(Guid supportRequestId)
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseResponseSupportModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllMyResponseAsync(supportRequestId)));
        }

        /// <summary>
        /// Lấy yêu cầu hỗ trợ bằng id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupportRequestById(Guid supportRequestId)
        {
            return Ok(new BaseResponseModel<ResponseSupportRequestModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetByIdAsync(supportRequestId)));
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
        /// Cập nhật trạng thái yêu cầu hỗ trợ thành đã gửi lên cấp cao hơn.
        /// </summary>
        [HttpPut("{supportRequestId}/escalated")]
        public async Task<IActionResult> ChangeSupportRequestStatusToEscalated(Guid supportRequestId)
        {
            await _supportRequestService.EscalateAsync(supportRequestId);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật trạng thái yêu cầu hỗ trợ thành công."));
        }

        /// <summary>
        /// Cập nhật trạng thái một yêu cầu hỗ trợ thành đã giải quyết.
        /// </summary>
        [HttpPut("{supportRequestId}/resolved")]
        public async Task<IActionResult> ChangeSupportRequestStatusToResolved(Guid supportRequestId)
        {
            await _supportRequestService.ResolveAsync(supportRequestId);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật trạng thái yêu cầu hỗ trợ thành công."));
        }

        /// <summary>
        /// Cập nhật trạng thái một yêu cầu hỗ trợ thành đã hủy.
        /// </summary>
        [HttpPut("{supportRequestId}/cancelled")]
        public async Task<IActionResult> ChangeSupportRequestStatusToCancelled(Guid supportRequestId)
        {
            await _supportRequestService.CancelAsync(supportRequestId);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật trạng thái yêu cầu hỗ trợ thành công."));
        }
    }
}

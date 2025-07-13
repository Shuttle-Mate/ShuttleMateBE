using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ResponseSupportModelViews;
using ShuttleMate.ModelViews.SupportRequestModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/support-request")]
    [ApiController]
    public class SupportRequestController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;

        public SupportRequestController(ISupportRequestService supportRequestService)
        {
            _supportRequestService = supportRequestService;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu hỗ trợ.
        /// </summary>
        /// <param name="category">Loại: TRANSPORT_ISSUE, TECHNICAL_ISSUE, PAYMENT_ISSUE, GENERAL_INQUIRY, OTHER (tùy chọn).</param>
        /// <param name="status">Trạng thái: IN_PROGRESS, RESPONSED, RESOLVED, CANCELLED (tùy chọn).</param>
        /// <param name="search">Từ khóa tìm kiếm trong tiêu đề hoặc nội dung (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 1).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpGet]
        public async Task<IActionResult> GetAllSupportRequests(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseSupportRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllAsync(category, status, search, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy danh sách yêu cầu hỗ trợ của tôi.
        /// </summary>
        /// <param name="category">Loại: TRANSPORT_ISSUE, TECHNICAL_ISSUE, PAYMENT_ISSUE, GENERAL_INQUIRY, OTHER (tùy chọn).</param>
        /// <param name="status">Trạng thái: IN_PROGRESS, RESPONSED, RESOLVED, CANCELLED (tùy chọn).</param>
        /// <param name="search">Từ khóa tìm kiếm trong tiêu đề hoặc nội dung (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpGet("my")]
        public async Task<IActionResult> GetAllMySupportRequests(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] bool sortAsc = false)
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseSupportRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllMyAsync(category, status, search, sortAsc)));
        }

        /// <summary>
        /// Lấy danh sách phản hồi của một yêu cầu hỗ trợ.
        /// </summary>
        [HttpGet("{id}/responses")]
        public async Task<IActionResult> GetAllResponsesOfSupportRequest(Guid id)
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseResponseSupportModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _supportRequestService.GetAllResponsesAsync(id)));
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu hỗ trợ.
        /// </summary>
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
        //[Authorize(Roles = "Student", "Parent")]
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
        /// Cập nhật trạng thái một yêu cầu hỗ trợ thành đã giải quyết.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpPut("{id}/resolved")]
        public async Task<IActionResult> ChangeSupportRequestStatusToResolved(Guid id)
        {
            await _supportRequestService.ResolveAsync(id);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật trạng thái yêu cầu hỗ trợ thành công."));
        }

        /// <summary>
        /// Hủy yêu cầu hỗ trợ.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpPut("{id}/cancelled")]
        public async Task<IActionResult> ChangeSupportRequestStatusToCancelled(Guid id)
        {
            await _supportRequestService.CancelAsync(id);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Hủy yêu cầu hỗ trợ thành công."));
        }
    }
}

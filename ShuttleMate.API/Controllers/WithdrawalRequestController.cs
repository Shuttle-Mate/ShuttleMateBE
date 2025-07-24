using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.WithdrawalRequestModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/withdrawal-request")]
    [ApiController]
    public class WithdrawalRequestController : ControllerBase
    {
        private readonly IWithdrawalRequestService _withdrawalRequestService;

        public WithdrawalRequestController(IWithdrawalRequestService withdrawalRequestService)
        {
            _withdrawalRequestService = withdrawalRequestService;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu hoàn tiền.
        /// </summary>
        /// <param name="status">Trạng thái: IN_PROGRESS, COMPLETED, REJECTED (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpGet]
        public async Task<IActionResult> GetAllWithdrawalRequests(
        [FromQuery] string? status,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseWithdrawalRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _withdrawalRequestService.GetAllAsync(status, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy danh sách yêu cầu hoàn tiền của tôi.
        /// </summary>
        /// <param name="status">Trạng thái: IN_PROGRESS, COMPLETED, REJECTED (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpGet("my")]
        public async Task<IActionResult> GetAllMyWithdrawalRequests(
        [FromQuery] string? status,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseWithdrawalRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _withdrawalRequestService.GetAllMyAsync(status, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu hoàn tiền.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWithdrawalRequestById(Guid id)
        {
            return Ok(new BaseResponseModel<ResponseWithdrawalRequestModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _withdrawalRequestService.GetByIdAsync(id)));
        }

        /// <summary>
        /// Tạo một yêu cầu hoàn tiền mới.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpPost]
        public async Task<IActionResult> CreateWithdrawalRequest(CreateWithdrawalRequestModel model)
        {
            await _withdrawalRequestService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới yêu cầu hoàn tiền thành công."));
        }

        /// <summary>
        /// Cập nhật một yêu cầu hoàn tiền.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateWithdrawalRequest(Guid id, UpdateWithdrawalRequestModel model)
        {
            await _withdrawalRequestService.UpdateAsync(id, model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật yêu cầu hoàn tiền thành công."
            ));
        }

        /// <summary>
        /// Cập nhật trạng thái một yêu cầu hoàn tiền thành đã hoàn thành.
        /// </summary>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpPatch("{id}/completed")]
        public async Task<IActionResult> ChangeWithdrawalRequestStatusToCompleted(Guid id)
        {
            await _withdrawalRequestService.CompleteAsync(id);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật trạng thái yêu cầu hoàn tiền thành công."));
        }

        /// <summary>
        /// Từ chối yêu cầu hoàn tiền.
        /// </summary>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpPatch("{id}/rejected")]
        public async Task<IActionResult> ChangeWithdrawalRequestStatusToRejected(Guid id, RejectWithdrawalRequestModel model)
        {
            await _withdrawalRequestService.RejectAsync(id, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Từ chối yêu cầu hoàn tiền thành công."));
        }

        /// <summary>
        /// Xóa một yêu cầu hoàn tiền.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWithdrawalRequest(Guid id)
        {
            await _withdrawalRequestService.DeleteAsync(id);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa yêu cầu hoàn tiền thành công."
            ));
        }
    }
}

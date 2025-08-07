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
        [FromQuery] Guid? userId,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseWithdrawalRequestModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _withdrawalRequestService.GetAllAsync(status, userId, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu hoàn tiền.
        /// </summary>
        [HttpGet("{withdrawalRequestId}")]
        public async Task<IActionResult> GetWithdrawalRequestById(Guid withdrawalRequestId)
        {
            return Ok(new BaseResponseModel<ResponseWithdrawalRequestModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _withdrawalRequestService.GetByIdAsync(withdrawalRequestId)));
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
        [HttpPatch("{withdrawalRequestId}")]
        public async Task<IActionResult> UpdateWithdrawalRequest(Guid withdrawalRequestId, UpdateWithdrawalRequestModel model)
        {
            await _withdrawalRequestService.UpdateAsync(withdrawalRequestId, model);
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
        [HttpPatch("{withdrawalRequestId}/completed")]
        public async Task<IActionResult> ChangeWithdrawalRequestStatusToCompleted(Guid withdrawalRequestId)
        {
            await _withdrawalRequestService.CompleteAsync(withdrawalRequestId);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật trạng thái yêu cầu hoàn tiền thành công."));
        }

        /// <summary>
        /// Từ chối yêu cầu hoàn tiền.
        /// </summary>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpPatch("{withdrawalRequestId}/rejected")]
        public async Task<IActionResult> ChangeWithdrawalRequestStatusToRejected(Guid withdrawalRequestId, RejectWithdrawalRequestModel model)
        {
            await _withdrawalRequestService.RejectAsync(withdrawalRequestId, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Từ chối yêu cầu hoàn tiền thành công."));
        }

        /// <summary>
        /// Xóa một yêu cầu hoàn tiền.
        /// </summary>
        //[Authorize(Roles = "Student", "Parent")]
        [HttpDelete("{withdrawalRequestId}")]
        public async Task<IActionResult> DeleteWithdrawalRequest(Guid withdrawalRequestId)
        {
            await _withdrawalRequestService.DeleteAsync(withdrawalRequestId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa yêu cầu hoàn tiền thành công."
            ));
        }
    }
}

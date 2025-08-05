using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.HistoryTicketModelView;
using ShuttleMate.ModelViews.TransactionModelView;
using ShuttleMate.Services.Services;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }
        /// <summary>
        /// Lấy tất cả các giao dịch của chính mình(người dùng)
        /// </summary>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        /// <param name="paymentStatus">trạng thái lần lượt là UNPAID, PAID, REFUNDED, CANCELED</param>
        /// <param name="orderCode">mã của giao dịch</param>
        /// <param name="createTime">true là tăng dần, false là giảm dần</param>
        /// <param name="description">mô tả</param>

        [HttpGet("my")]
        public async Task<IActionResult> GetAllForUserAsync(int page = 0, int pageSize = 10, string? paymentStatus = null, int? orderCode = null, string? description = null, bool? createTime = null)
        {
            var transactions = await _transactionService.GetAllForUserAsync(page,pageSize, paymentStatus, orderCode, description, createTime);

            return Ok(new BaseResponseModel<BasePaginatedList<TransactionResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: transactions
            ));
        }
        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetById(Guid transactionId)
        {
            var transactions = await _transactionService.GetById(transactionId);

            return Ok(new BaseResponseModel<TransactionResponseModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: transactions
            ));
        }
        /// <summary>
        /// Lấy tất cả các giao dịch(Admin)
        /// </summary>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        /// <param name="paymentStatus">trạng thái lần lượt là UNPAID, PAID, REFUNDED, CANCELED</param>
        /// <param name="orderCode">mã của giao dịch</param>
        /// <param name="createTime">true là tăng dần, false là giảm dần</param>
        /// <param name="description">mô tả</param>

        [HttpGet]
        public async Task<IActionResult> GetAllForAdminAsync(int page = 0, int pageSize = 10,  string? paymentStatus = null, int? orderCode = null, string? description = null, bool? createTime = null)
        {
            var transactions = await _transactionService.GetAllForAdminAsync(page,pageSize, paymentStatus, orderCode, description, createTime);

            return Ok(new BaseResponseModel<BasePaginatedList<TransactionAdminResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: transactions
            ));
        }

    }
}

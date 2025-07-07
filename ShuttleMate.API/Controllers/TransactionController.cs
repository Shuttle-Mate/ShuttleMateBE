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
        /// <param name="paymentMethodEnum">Loại thanh toán lần lượt là PayOs = 0, VNPay = 1</param>
        /// <param name="paymentStatus">trạng thái lần lượt là Unpaid = 0, Paid = 1, Refunded = 2, Canceled = 3</param>
        /// <param name="orderCode">mã của giao dịch</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần</param>
        /// <param name="description">mô tả</param>

        [HttpGet("my")]
        public async Task<IActionResult> GetAllForUserAsync(PaymentMethodEnum? paymentMethodEnum, PaymentStatus? paymentStatus = null, int? orderCode = null, string? description = null, bool? CreateTime = null)
        {
            var transactions = await _transactionService.GetAllForUserAsync(paymentMethodEnum, paymentStatus, orderCode, description, CreateTime);

            return Ok(new BaseResponseModel<IEnumerable<TransactionResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: transactions
            ));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.HistoryTicketModelView;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using ShuttleMate.Services.Services;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;
using System.Text.Json;


namespace ShuttleMate.API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class HistoryTicketController : ControllerBase
    {
        private IHistoryTicketService _historyTicketService;
        public HistoryTicketController(IHistoryTicketService historyTicketService)
        {
            _historyTicketService = historyTicketService;
        }

        /// <summary>
        /// Lấy tất cả các vé của chính mình(người dùng)
        /// </summary>
        /// <param name="status">trạng thái(UNPAID, PAID, CANCELLED, USED) (tuỳ chọn)</param>
        /// <param name="ticketType">Loại vé(WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO) (tuỳ chọn).</param>
        /// <param name="purchaseAt">Thời gian đặt vé (tuỳ chọn).</param>
        /// <param name="createTime">true là tăng dần, false là giảm dần.(tuỳ chọn).</param>
        /// <param name="validFrom">tra theo thời gian có hiệu lực tuỳ chọn).</param>
        /// <param name="validUntil">tra theo thời gian hết hiệu lực (tuỳ chọn).</param>
        /// <param name="ticketId">tra theo vé (tuỳ chọn).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllForUserAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? purchaseAt = null, bool? createTime = null, DateOnly? validFrom = null, DateOnly? validUntil = null, Guid? ticketId = null, string? ticketType = null)
        {
            var tickets = await _historyTicketService.GetAllForUserAsync(page, pageSize, status, purchaseAt, createTime, validFrom, validUntil, ticketId, ticketType);

            return Ok(new BaseResponseModel<BasePaginatedList<HistoryTicketResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        /// <summary>
        /// Lấy tất cả các vé của học sinh(phụ huynh)
        /// </summary>
        /// <param name="status">trạng thái(UNPAID, PAID, CANCELLED, USED) (tuỳ chọn).</param>
        /// <param name="purchaseAt">Thời gian đặt vé (tuỳ chọn).</param>
        /// <param name="createTime">true là tăng dần, false là giảm dần (tuỳ chọn).</param>
        /// <param name="validFrom">tra theo thời gian có hiệu lực (tuỳ chọn).</param>
        /// <param name="validUntil">tra theo thời gian hết hiệu lực (tuỳ chọn).</param>
        /// <param name="ticketId">tra theo vé</param>
        /// <param name="ticketType">Loại vé(WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO) (tuỳ chọn).</param>
        /// <param name="studentId">Id của học sinh (tuỳ chọn).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet("student")]
        public async Task<IActionResult> GetAllForParentAsync(int page = 0, int pageSize = 10, string? ticketType = null, string? status = null, DateTime? purchaseAt = null, bool? createTime = null, DateOnly? validFrom = null, DateOnly? validUntil = null, Guid? ticketId = null, Guid? studentId = null)
        {
            var tickets = await _historyTicketService.GetAllForParentAsync(page, pageSize, status, purchaseAt, createTime, validFrom, validUntil, ticketId, studentId, ticketType);

            return Ok(new BaseResponseModel<BasePaginatedList<HistoryTicketResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        /// <summary>
        /// Lấy tất cả các vé(Admin)
        /// </summary>
        /// <param name="status">trạng thái(UNPAID, PAID, CANCELLED, USED) (tuỳ chọn).</param>
        /// <param name="purchaseAt">Thời gian đặt vé (tuỳ chọn).</param>
        /// <param name="createTime">true là tăng dần, false là giảm dần (tuỳ chọn).</param>
        /// <param name="validFrom">tra theo thời gian có hiệu lực (tuỳ chọn).</param>
        /// <param name="validUntil">tra theo thời gian hết hiệu lực (tuỳ chọn).</param>
        /// <param name="ticketId">tra theo vé (tuỳ chọn).</param>
        /// <param name="userId">tra theo người mua (tuỳ chọn).</param>
        /// <param name="ticketType">Loại vé(WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO) (tuỳ chọn).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllForAdminAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? purchaseAt = null, bool? createTime = null, DateOnly? validFrom = null, DateOnly? validUntil = null, Guid? userId = null, Guid? ticketId = null, string? ticketType = null)
        {
            var tickets = await _historyTicketService.GetAllForAdminAsync(page, pageSize, status, purchaseAt, createTime, validFrom, validUntil, userId, ticketId, ticketType);

            return Ok(new BaseResponseModel<BasePaginatedList<HistoryTicketAdminResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        /// <summary>
        /// Lấy status của history ticket
        /// </summary>
        [HttpGet("response-historyticket-status")]
        public async Task<IActionResult> ResponseHistoryTicketStatus(Guid historyTicketId)
        {
            string response = await _historyTicketService.ResponseHistoryTicketStatus(historyTicketId);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: response
            ));
        }
        /// <summary>
        /// Mua vé (PAYOS): ValidFrom ko áp dụng cho vé loại SEMESTER_ONE, SEMESTER_TWO(STUDENT)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateHistoryTicket(CreateHistoryTicketModel model)
        {
            CreateHistoryTicketResponse response = await _historyTicketService.CreateHistoryTicket(model);

            return Ok(new BaseResponseModel<CreateHistoryTicketResponse>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: response
            ));
        }
        /// <summary>
        /// Mua vé (PAYOS): ValidFrom ko áp dụng cho vé loại SEMESTER_ONE, SEMESTER_TWO(PARENT)
        /// </summary>
        [HttpPost("buy-ticket-for-parent")]
        public async Task<IActionResult> CreateHistoryTicketForParent(CreateHistoryTicketForParentModel model)
        {
            CreateHistoryTicketResponse response = await _historyTicketService.CreateHistoryTicketForParent(model);

            return Ok(new BaseResponseModel<CreateHistoryTicketResponse>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: response
            ));
        }

        /// <summary>
        /// Hàm xử lí sau khi thanh toán(PAYOS)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("payos-callback")]
        public async Task<IActionResult> PayOSCallback([FromBody] PayOSWebhookRequest request)
        {
            try
            {
                string jsonRequest = JsonSerializer.Serialize(request);
                Console.WriteLine($"📌 Received Webhook Data: {jsonRequest}");
                //Console.WriteLine($"📌 Signature: {signature}");

                // Nếu request null, trả về lỗi
                if (request == null || request.data == null)
                {
                    return BadRequest(new { message = "Dữ liệu webhook không hợp lệ" });
                }

                // 🚀 Nếu request từ PayOS kiểm tra Webhook, bỏ qua xử lý nhưng vẫn trả về 200 OK
                if (request.data.orderCode == null)
                {
                    Console.WriteLine("📌 PayOS Webhook Verification - Skipping Processing");
                    return Ok(new { message = "Webhook verified successfully" });
                }

                // Xử lý khi có orderCode thật từ PayOS
                await _historyTicketService.PayOSCallback(request);
                return Ok(new { message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Webhook Error: {ex.Message}");
                return StatusCode(500, new { message = "Internal Server Error", error = ex.Message });
            }
        }

        ///// <summary>
        ///// Mua vé (ZALOPAY)
        ///// </summary>

        //[HttpPost("create_payment_zalopay")]
        //public async Task<IActionResult> CreateHistoryTicketZaloPay(CreateZaloPayOrderModel model)
        //{
        //    string linkPayOS = await _historyTicketService.CreateZaloPayOrder(model);

        //    return Ok(new BaseResponseModel<string>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: linkPayOS
        //    ));
        //}
    }
}

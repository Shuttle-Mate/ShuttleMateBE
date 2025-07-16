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
    [Route("api/history-ticket")]
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
        /// <param name="ticketType">Loại vé(SINGLE_RIDE, DAY_PASS, WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO) (tuỳ chọn).</param>
        /// <param name="PurchaseAt">Thời gian đặt vé (tuỳ chọn).</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần.(tuỳ chọn).</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực tuỳ chọn).</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực (tuỳ chọn).</param>
        /// <param name="ticketId">tra theo vé (tuỳ chọn).</param>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllForUserAsync(string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? ticketId = null, string? ticketType = null)
        {
            var tickets = await _historyTicketService.GetAllForUserAsync(status, PurchaseAt, CreateTime, ValidFrom, ValidUntil, ticketId, ticketType);

            return Ok(new BaseResponseModel<IEnumerable<HistoryTicketResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        /// <summary>
        /// Lấy tất cả các vé của học sinh(phụ huynh)
        /// </summary>
        /// <param name="status">trạng thái(UNPAID, PAID, CANCELLED, USED) (tuỳ chọn).</param>
        /// <param name="PurchaseAt">Thời gian đặt vé (tuỳ chọn).</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần (tuỳ chọn).</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực (tuỳ chọn).</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực (tuỳ chọn).</param>
        /// <param name="ticketId">tra theo vé</param>
        /// <param name="ticketType">Loại vé(SINGLE_RIDE, DAY_PASS, WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO) (tuỳ chọn).</param>
        /// <param name="studentId">Id của học sinh (tuỳ chọn).</param>
        [HttpGet("student")]
        public async Task<IActionResult> GetAllForParentAsync(string? ticketType = null, string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? ticketId = null, Guid? studentId = null)
        {
            var tickets = await _historyTicketService.GetAllForParentAsync(status, PurchaseAt, CreateTime, ValidFrom, ValidUntil, ticketId, studentId, ticketType);

            return Ok(new BaseResponseModel<IEnumerable<HistoryTicketResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        /// <summary>
        /// Lấy tất cả các vé(Admin)
        /// </summary>
        /// <param name="status">trạng thái(UNPAID, PAID, CANCELLED, USED) (tuỳ chọn).</param>
        /// <param name="PurchaseAt">Thời gian đặt vé (tuỳ chọn).</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần (tuỳ chọn).</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực (tuỳ chọn).</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực (tuỳ chọn).</param>
        /// <param name="ticketId">tra theo vé (tuỳ chọn).</param>
        /// <param name="userId">tra theo người mua (tuỳ chọn).</param>
        /// <param name="ticketType">Loại vé(SINGLE_RIDE, DAY_PASS, WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO) (tuỳ chọn).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllForAdminAsync(string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? userId = null, Guid? ticketId = null, string? ticketType = null)
        {
            var tickets = await _historyTicketService.GetAllForAdminAsync(status, PurchaseAt, CreateTime, ValidFrom, ValidUntil, userId, ticketId, ticketType);

            return Ok(new BaseResponseModel<IEnumerable<HistoryTicketAdminResponseModel>>(
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
        /// Mua vé (PAYOS)
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
        /// Hàm xử lí sau khi thanh toán(PAYOS)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("payos_callback")]
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

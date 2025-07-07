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
    [Route("api/[controller]")]
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
        /// <param name="status">trạng thái vé lần lượt là book = 0, Paid = 1, Cancelled = 2</param>
        /// <param name="ticketType">loại vé lần lượt là SingleRide = 0, DayPass = 1, Weekly = 2, Monthly = 3, Semester = 4</param>
        /// <param name="PurchaseAt">Thời gian đặt vé</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực</param>
        /// <param name="ticketId">tra theo vé</param>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllForUserAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? ticketId = null, TicketTypeEnum? ticketType = null)
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
        /// <param name="status">trạng thái vé lần lượt là book = 0, Paid = 1, Cancelled = 2</param>
        /// <param name="PurchaseAt">Thời gian đặt vé</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực</param>
        /// <param name="ticketId">tra theo vé</param>
        [HttpGet("studentId")]
        public async Task<IActionResult> GetAllForParentAsync(TicketTypeEnum? ticketType = null, HistoryTicketStatus ? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? ticketId = null, Guid? studentId = null )
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
        /// <param name="status">trạng thái vé lần lượt là book = 0, Paid = 1, Cancelled = 2</param>
        /// <param name="PurchaseAt">Thời gian đặt vé</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực</param>
        /// <param name="ticketId">tra theo vé</param>
        /// <param name="userId">tra theo người mua</param>
        [HttpGet]
        public async Task<IActionResult> GetAllForAdminAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? userId = null, Guid? ticketId = null, TicketTypeEnum? ticketType = null)
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

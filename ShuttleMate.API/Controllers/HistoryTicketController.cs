using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.HistoryTicketModelView;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using ShuttleMate.Services.Services;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

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
        /// Lấy tất cả các vé
        /// </summary>
        /// <param name="status">trạng thái vé lần lượt là book = 0, Paid = 1, Cancelled = 2</param>
        /// <param name="PurchaseAt">Thời gian đặt vé</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực</param>
        /// <param name="ticketId">tra theo vé</param>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllForUserAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? ticketId = null)
        {
            var tickets = await _historyTicketService.GetAllForUserAsync(status, PurchaseAt, CreateTime, ValidFrom, ValidUntil, ticketId);

            return Ok(new BaseResponseModel<IEnumerable<HistoryTicketResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        /// <summary>
        /// Lấy tất cả các vé
        /// </summary>
        /// <param name="status">trạng thái vé lần lượt là book = 0, Paid = 1, Cancelled = 2</param>
        /// <param name="PurchaseAt">Thời gian đặt vé</param>
        /// <param name="CreateTime">true là tăng dần, false là giảm dần</param>
        /// <param name="ValidFrom">tra theo thời gian có hiệu lực</param>
        /// <param name="ValidUntil">tra theo thời gian hết hiệu lực</param>
        /// <param name="ticketId">tra theo vé</param>
        /// <param name="userId">tra theo người mua</param>
        [HttpGet]
        public async Task<IActionResult> GetAllForAdminAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? userId = null, Guid? ticketId = null)
        {
            var tickets = await _historyTicketService.GetAllForAdminAsync(status, PurchaseAt, CreateTime, ValidFrom, ValidUntil, userId, ticketId);

            return Ok(new BaseResponseModel<IEnumerable<HistoryTicketResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        [HttpPost]
        public async Task<IActionResult> CreateHistoryTicket(CreateHistoryTicketModel model)
        {
            string linkPayOS = await _historyTicketService.CreateHistoryTicket(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: linkPayOS
            ));
        }
    }
}

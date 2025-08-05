using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using ShuttleMate.Services.Services;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.API.Controllers
{
    [Route("api/ticket")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        /// <summary>
        /// Lấy tất cả các vé.
        /// </summary>
        /// <param name="type">Loại vé(WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO)(tuỳ chọn).</param>
        /// <param name="search">Tìm theo RouteName, RouteCode, SchoolName (tuỳ chọn).</param>
        /// <param name="price">Sắp xếp theo giá: true là tăng dần, false là giảm dần (tuỳ chọn).</param>
        /// <param name="lowerBound">Cận dưới: khi chỉ có một cận thì >= nó, khi có hai cận thì >= nó và bé hơn hoặc = cận trên (tuỳ chọn).</param>
        /// <param name="upperBound">Cận trên: khi chỉ có một cận thì >= cận dưới, khi có hai cận thì >= nó và bé hơn hoặc = nó (tuỳ chọn).</param>
        /// <param name="routeId">Lọc theo tuyến (tuỳ chọn).</param>
        /// <param name="schoolId">Lọc theo trường (tuỳ chọn).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllTickets(int page = 0, int pageSize = 10, string? type = null, string? search = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null, Guid? routeId = null, Guid? schoolId = null)
        {
            var tickets = await _ticketService.GetAllAsync(page, pageSize, type, search, price, lowerBound, upperBound, routeId, schoolId);

            return Ok(new BaseResponseModel<BasePaginatedList<TicketResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicketById(Guid id)
        {
            var ticket = await _ticketService.GetById(id);

            return Ok(new BaseResponseModel<TicketResponseModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: ticket
            ));
        }
        /// <summary>
        /// Type(WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTicket(CreateTicketModel model)
        {
            await _ticketService.CreateTicket(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo loại vé thành công!"
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateTicket(UpdateTicketModel model)
        {
            await _ticketService.UpdateTicket(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật loại vé thành công!"
            ));
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteTicket(Guid ticketId)
        {
            await _ticketService.DeleteTicket(ticketId);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa loại vé thành công!"
            ));
        }
    }
}

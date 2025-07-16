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
    [Route("api/ticket-type")]
    [ApiController]
    public class TicketTypeController : ControllerBase
    {
        private readonly ITicketTypeService _ticketTypeService;
        public TicketTypeController(ITicketTypeService ticketTypeService)
        {
            _ticketTypeService = ticketTypeService;
        }

        /// <summary>
        /// Lấy tất cả các vé.
        /// </summary>
        /// <param name="type">Loại vé(SINGLE_RIDE, DAY_PASS, WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO)(tuỳ chọn).</param>
        /// <param name="routeName">Tên tuyến (tuỳ chọn).</param>
        /// <param name="price">Sắp xếp theo giá: true là tăng dần, false là giảm dần (tuỳ chọn).</param>
        /// <param name="lowerBound">Cận dưới: khi chỉ có một cận thì >= nó, khi có hai cận thì >= nó và bé hơn hoặc = cận trên (tuỳ chọn).</param>
        /// <param name="upperBound">Cận trên: khi chỉ có một cận thì >= cận dưới, khi có hai cận thì >= nó và bé hơn hoặc = nó (tuỳ chọn).</param>
        /// <param name="routeId">Lọc theo tuyến (tuỳ chọn).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllTicketTypes(int page = 0, int pageSize = 10, string? type = null, string? routeName = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null, Guid? routeId = null)
        {
            var tickets = await _ticketTypeService.GetAllAsync(page, pageSize, type, routeName, price, lowerBound, upperBound, routeId);

            return Ok(new BaseResponseModel<BasePaginatedList<TicketTypeResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: tickets
            ));
        }
        [HttpGet("id")]
        public async Task<IActionResult> GetTicketTypeById(Guid id)
        {
            var ticket = await _ticketTypeService.GetById(id);

            return Ok(new BaseResponseModel<TicketTypeResponseModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: ticket
            ));
        }
        /// <summary>
        /// Type(SINGLE_RIDE, DAY_PASS, WEEKLY, MONTHLY, SEMESTER_ONE, SEMESTER_TWO)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTicketType(CreateTicketTypeModel model)
        {
            await _ticketTypeService.CreateTicketType(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo loại vé thành công!"
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateTicketType(UpdateTicketTypeModel model)
        {
            await _ticketTypeService.UpdateTicketType(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật loại vé thành công!"
            ));
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteTicketType(DeleteTicketTypeModel model)
        {
            await _ticketTypeService.DeleteTicketType(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa loại vé thành công!"
            ));
        }
    }
}

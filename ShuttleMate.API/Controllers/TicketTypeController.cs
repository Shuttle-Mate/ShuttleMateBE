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
    [Route("api/[controller]")]
    [ApiController]
    public class TicketTypeController : ControllerBase
    {
        private readonly ITicketTypeService _ticketTypeService;
        /// <summary>
        /// Lấy tất cả các vé
        /// </summary>
        /// <param name="type">loại vé lần lượt là SingleRide = 0, DayPass = 1, Weekly = 2, Monthly = 3</param>
        /// <param name="routeName">tên tuyến</param>
        /// <param name="price">true là tăng dần, false là giảm dần</param>
        /// <param name="lowerBound">Cận dưới: khi 1 cận thì >= nó, khi 2 cận thì >= nó và <= cận trên</param>
        /// <param name="upperBound">Cận trên: khi 1 cận thì >= cận dưới, khi 2 cận thì >= nó và <= nó</param>
        [HttpGet]
        public async Task<IActionResult> GetAllTicketTypes(TicketTypeEnum? type, string? routeName = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null)
        {
            var tickets = await _ticketTypeService.GetAllAsync(type, routeName, price, lowerBound, upperBound);

            return Ok(new BaseResponseModel<IEnumerable<TicketTypeResponseModel>>(
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
    }
}

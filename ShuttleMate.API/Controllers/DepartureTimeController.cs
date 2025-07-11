using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.DepartureTimeModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/departure-time")]
    [ApiController]
    public class DepartureTimeController : ControllerBase
    {
        private readonly IDepartureTimeService _departureTimeService;

        public DepartureTimeController(IDepartureTimeService departureTimeService)
        {
            _departureTimeService = departureTimeService;
        }

        /// <summary>
        /// Tạo thời gian khởi hành của tuyến.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDepartureTime(CreateDepartureTimeModel model)
        {
            await _departureTimeService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới thời gian khởi hành thành công."));
        }

        /// <summary>
        /// Cập nhật thời gian khởi hành của tuyến.
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateDepartureTime(UpdateDepartureTimeModel model)
        {
            await _departureTimeService.UpdateAsync(model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật thời gian khởi hành thành công."));
        }
    }
}

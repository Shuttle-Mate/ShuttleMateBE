using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/schedule")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        /// <summary>
        /// Lấy danh sách lịch trình ngày hiện tại của tài xế.
        /// </summary>
        //[Authorize(Roles = "Driver")]
        [HttpGet("today")]
        public async Task<IActionResult> GetAllTodaySchedulesForDriver()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseTodayScheduleForDriverModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _scheduleService.GetAllTodayAsync()));
        }

        /// <summary>
        /// Tạo thời gian khởi hành của tuyến.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDepartureTime(CreateScheduleModel model)
        {
            await _scheduleService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới thời gian khởi hành thành công."));
        }

        /// <summary>
        /// Cập nhật thời gian khởi hành của tuyến.
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateDepartureTime(UpdateScheduleModel model)
        {
            await _scheduleService.UpdateAsync(model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật thời gian khởi hành thành công."));
        }
    }
}

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
        /// Tạo lịch trình của tuyến.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDepartureTime(CreateScheduleModel model)
        {
            await _scheduleService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới lịch trình thành công."));
        }

        /// <summary>
        /// Cập nhật một lịch trình.
        /// </summary>
        [HttpPut("{scheduleId}")]
        public async Task<IActionResult> UpdateDepartureTime(Guid scheduleId, UpdateScheduleModel model)
        {
            await _scheduleService.UpdateAsync(scheduleId, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật lịch trình thành công."));
        }
    }
}

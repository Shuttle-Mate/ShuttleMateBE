using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ScheduleModelViews;
using ShuttleMate.Services.Services;

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
        /// Lấy chi tiết lịch trình.
        /// </summary>
        [HttpGet("{scheduleId}")]
        public async Task<IActionResult> GetScheduleById(Guid scheduleId)
        {
            return Ok(new BaseResponseModel<ResponseScheduleModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _scheduleService.GetByIdAsync(scheduleId)));
        }

        /// <summary>
        /// Tạo lịch trình của tuyến.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSchedule(CreateScheduleModel model)
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
        public async Task<IActionResult> UpdateSchedule(Guid scheduleId, UpdateScheduleModel model)
        {
            await _scheduleService.UpdateAsync(scheduleId, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật lịch trình thành công."));
        }

        /// <summary>
        /// Xóa một lịch trình.
        /// </summary>
        ///
        [HttpDelete("{scheduleId}")]
        public async Task<IActionResult> DeleteSchedule(Guid scheduleId)
        {
            await _scheduleService.DeleteAsync(scheduleId);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa lịch trình thành công."));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ScheduleModelViews;
using ShuttleMate.ModelViews.ScheduleOverrideModelView;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/schedule-override")]
    [ApiController]
    public class ScheduleOverrideController : ControllerBase
    {
        private readonly IScheduleOverrideService _scheduleOverrideService;

        public ScheduleOverrideController(IScheduleOverrideService scheduleOverrideService)
        {
            _scheduleOverrideService = scheduleOverrideService;
        }

        /// <summary>
        /// Tạo lịch trình thay thế.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateScheduleOverride(CreateScheduleOverrideModel model)
        {
            await _scheduleOverrideService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới lịch trình thay thế thành công."));
        }

        /// <summary>
        /// Cập nhật một lịch trình thay thế.
        /// </summary>
        [HttpPut("{scheduleOverrideId}")]
        public async Task<IActionResult> UpdateScheduleOverride(Guid scheduleOverrideId, UpdateScheduleOverrideModel model)
        {
            await _scheduleOverrideService.UpdateAsync(scheduleOverrideId, model);
            return Ok(new BaseResponseModel<string?>(
               statusCode: StatusCodes.Status200OK,
               code: ResponseCodeConstants.SUCCESS,
               message: "Cập nhật lịch trình thay thế thành công."));
        }

        /// <summary>
        /// Xóa một lịch trình thay thế.
        /// </summary>
        [HttpDelete("{scheduleOverrideId}")]
        public async Task<IActionResult> DeleteSchedule(Guid scheduleOverrideId, DeleteScheduleOverrideModel model)
        {
            await _scheduleOverrideService.DeleteAsync(scheduleOverrideId, model);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa lịch trình thay thế thành công."));
        }
    }
}

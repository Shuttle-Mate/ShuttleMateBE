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
        /// Lấy danh sách lịch trình hôm nay của tài xế.
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
        /// Lấy danh sách lịch trình theo tuyến.
        /// </summary>
        /// <param name="routeId">Id của tuyến (bắt buộc).</param>
        /// <param name="from">Lọc từ ngày (bắt buộc).</param>
        /// <param name="to">Lọc đến ngày (bắt buộc).</param>
        /// <param name="direction">Hướng của tuyến: IN_BOUND, OUT_BOUND (tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp giảm dần theo giờ khởi hành (true, mặc định) hoặc giảm dần (false).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        //[Authorize(Roles = "Admin, Operator")]
        [HttpGet]
        public async Task<IActionResult> GetAllSchedules(
        [FromQuery] Guid routeId,
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string? direction,
        [FromQuery] bool sortAsc = true,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseOldScheduleModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _scheduleService.GetAllAsync(routeId, from, to, direction, sortAsc, page, pageSize)));
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
        [HttpDelete("{scheduleId}")]
        public async Task<IActionResult> DeleteSchedule(Guid scheduleId, string dayOfWeek)
        {
            await _scheduleService.DeleteAsync(scheduleId, dayOfWeek);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa lịch trình thành công."));
        }
    }
}

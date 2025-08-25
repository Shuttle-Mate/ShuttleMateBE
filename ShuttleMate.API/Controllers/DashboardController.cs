namespace ShuttleMate.API.Controllers
{
    using global::ShuttleMate.Contract.Services.Interfaces;
    using global::ShuttleMate.Core.Bases;
    using global::ShuttleMate.Core.Constants;
    using global::ShuttleMate.ModelViews.DashboardModelViews;
    using Microsoft.AspNetCore.Mvc;

    namespace ShuttleMate.API.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class DashboardController : ControllerBase
        {
            private readonly IDashboardService _dashboardService;

            public DashboardController(IDashboardService dashboardService)
            {
                _dashboardService = dashboardService;
            }

            [HttpGet("overview")]
            public async Task<IActionResult> GetOverview()
            {
                var data = await _dashboardService.GetOverviewAsync();
                return Ok(new BaseResponseModel<DashboardOverviewModel>(
                    statusCode: StatusCodes.Status200OK,
                    code: ResponseCodeConstants.SUCCESS,
                    data: data
                ));
            }

            [HttpGet("trip-statistics")]
            public async Task<IActionResult> GetTripStatistics()
            {
                var data = await _dashboardService.GetTripStatisticsAsync();
                return Ok(new BaseResponseModel<TripStatisticModel>(
                    statusCode: StatusCodes.Status200OK,
                    code: ResponseCodeConstants.SUCCESS,
                    data: data
                ));
            }

            [HttpGet("attendance-statistics")]
            public async Task<IActionResult> GetAttendanceStatistics()
            {
                var data = await _dashboardService.GetAttendanceStatisticsAsync();
                return Ok(new BaseResponseModel<AttendanceStatisticsModel>(
                    statusCode: StatusCodes.Status200OK,
                    code: ResponseCodeConstants.SUCCESS,
                    data: data
                ));
            }

            [HttpGet("transaction-statistics")]
            public async Task<IActionResult> GetTransactionStatistics(DateOnly? fromDate, DateOnly? toDate)
            {
                var data = await _dashboardService.GetTransactionStatisticsAsync(fromDate, toDate);
                return Ok(new BaseResponseModel<TransactionStatisticsModel>(
                    statusCode: StatusCodes.Status200OK,
                    code: ResponseCodeConstants.SUCCESS,
                    data: data
                ));
            }
        }
    }
}

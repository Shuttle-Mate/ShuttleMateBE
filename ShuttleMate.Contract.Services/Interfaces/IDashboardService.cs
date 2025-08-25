using ShuttleMate.ModelViews.DashboardModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<TripStatisticModel> GetTripStatisticsAsync();
        Task<AttendanceStatisticsModel> GetAttendanceStatisticsAsync();
        Task<TransactionStatisticsModel> GetTransactionStatisticsAsync(DateOnly? fromDate, DateOnly? toDate);
        Task<DashboardOverviewModel> GetOverviewAsync();
    }
}

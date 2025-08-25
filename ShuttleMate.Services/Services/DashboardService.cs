using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.Enum;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.ModelViews.DashboardModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardOverviewModel> GetOverviewAsync()
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var tripRepo = _unitOfWork.GetRepository<Trip>();
            var shuttleRepo = _unitOfWork.GetRepository<Shuttle>();
            var schoolRepo = _unitOfWork.GetRepository<School>();
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();

            var userCount = await userRepo.Entities.CountAsync(x => !x.DeletedTime.HasValue);
            var studentCount = await userRepo.Entities.CountAsync(x => x.UserRoles.Any(r => r.Role.Name == "STUDENT") && !x.DeletedTime.HasValue);
            var driverCount = await userRepo.Entities.CountAsync(x => x.UserRoles.Any(r => r.Role.Name == "DRIVER") && !x.DeletedTime.HasValue);
            var tripCount = await tripRepo.Entities.CountAsync(x => !x.DeletedTime.HasValue);
            var shuttleCount = await shuttleRepo.Entities.CountAsync(x => !x.DeletedTime.HasValue);
            var schoolCount = await schoolRepo.Entities.CountAsync(x => !x.DeletedTime.HasValue);
            var totalTransaction = await transactionRepo.Entities.CountAsync(x => !x.DeletedTime.HasValue);
            var revenue = await transactionRepo.Entities
                .Where(x => x.Status == GeneralEnum.PaymentStatus.PAID && !x.DeletedTime.HasValue)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            return new DashboardOverviewModel
            {
                TotalUser = userCount,
                TotalStudent = studentCount,
                TotalDriver = driverCount,
                TotalTrip = tripCount,
                TotalShuttle = shuttleCount,
                TotalSchool = schoolCount,
                TotalRevenue = revenue,
                TotalTransaction = totalTransaction
            };
        }

        public async Task<TripStatisticModel> GetTripStatisticsAsync()
        {
            var tripRepo = _unitOfWork.GetRepository<Trip>();
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var today = DateOnly.FromDateTime(vietnamNow);
            var firstDayOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);

            var totalTripToday = await tripRepo.Entities.CountAsync(x => x.TripDate == today && !x.DeletedTime.HasValue);
            var totalTripThisWeek = await tripRepo.Entities.CountAsync(x => x.TripDate >= firstDayOfWeek && x.TripDate < firstDayOfWeek.AddDays(7) && !x.DeletedTime.HasValue);
            var totalTripThisMonth = await tripRepo.Entities.CountAsync(x => x.TripDate >= firstDayOfMonth && x.TripDate < firstDayOfMonth.AddMonths(1) && !x.DeletedTime.HasValue);

            // Chart: số chuyến mỗi ngày trong 7 ngày gần nhất
            var fromDate = today.AddDays(-6);
            
            // Lấy dữ liệu thô về trước
            var tripList = await tripRepo.Entities
                .Where(x => x.TripDate >= fromDate && x.TripDate <= today && !x.DeletedTime.HasValue)
                .ToListAsync();

            // Group và tính toán trên bộ nhớ
            var tripChart = tripList
                .GroupBy(x => x.TripDate)
                .Select(g => new TripChartData
                {
                    Date = g.Key.ToDateTime(TimeOnly.MinValue),
                    TripCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return new TripStatisticModel
            {
                TotalTripToday = totalTripToday,
                TotalTripThisWeek = totalTripThisWeek,
                TotalTripThisMonth = totalTripThisMonth,
                TripChart = tripChart
            };
        }

        public async Task<AttendanceStatisticsModel> GetAttendanceStatisticsAsync()
        {
            var attendanceRepo = _unitOfWork.GetRepository<Attendance>();
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var today = DateOnly.FromDateTime(vietnamNow);

            var totalCheckInToday = await attendanceRepo.Entities.CountAsync(x => x.CheckInTime.Date == vietnamNow && !x.DeletedTime.HasValue);
            var totalCheckOutToday = await attendanceRepo.Entities.CountAsync(x => x.CheckOutTime != null && x.CheckOutTime.Date == vietnamNow && !x.DeletedTime.HasValue);
            var totalAbsentToday = await attendanceRepo.Entities.CountAsync(x => x.CheckInTime.Date != vietnamNow && !x.DeletedTime.HasValue);

            // Chart: điểm danh 7 ngày gần nhất
            var fromDate = vietnamNow.AddDays(-6);
            var attendanceChart = await attendanceRepo.Entities
                .Where(x => x.CheckInTime >= fromDate && !x.DeletedTime.HasValue)
                .GroupBy(x => x.CheckInTime.Date)
                .Select(g => new AttendanceChartData
                {
                    Date = g.Key,
                    CheckInCount = g.Count(a => a.CheckInTime != null),
                    CheckOutCount = g.Count(a => a.CheckOutTime != null),
                    AbsentCount = 0 // Nếu muốn tính absent thực tế cần logic khác
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new AttendanceStatisticsModel
            {
                TotalCheckInToday = totalCheckInToday,
                TotalCheckOutToday = totalCheckOutToday,
                TotalAbsentToday = totalAbsentToday,
                AttendanceChart = attendanceChart
            };
        }

        public async Task<TransactionStatisticsModel> GetTransactionStatisticsAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var today = DateOnly.FromDateTime(vietnamNow);

            // Xác định khoảng ngày
            var chartToDate = toDate ?? today;
            var chartFromDate = fromDate ?? chartToDate.AddDays(-6);

            var totalTransaction = await transactionRepo.Entities.CountAsync(x => !x.DeletedTime.HasValue);
            var totalRevenue = await transactionRepo.Entities
                .Where(x => x.Status == GeneralEnum.PaymentStatus.PAID && !x.DeletedTime.HasValue)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;
            var paidTransaction = await transactionRepo.Entities.CountAsync(x => x.Status == GeneralEnum.PaymentStatus.PAID && !x.DeletedTime.HasValue);
            var unpaidTransaction = await transactionRepo.Entities.CountAsync(x => x.Status == GeneralEnum.PaymentStatus.UNPAID && !x.DeletedTime.HasValue);
            var refundedTransaction = await transactionRepo.Entities.CountAsync(x => x.Status == GeneralEnum.PaymentStatus.REFUNDED && !x.DeletedTime.HasValue);

            // Lấy toàn bộ transaction trong khoảng thời gian về bộ nhớ
            var transactionList = await transactionRepo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .ToListAsync();

            // Chuyển đổi và filter trên bộ nhớ
            var filteredTransactions = transactionList
                .Where(x =>
                    (DateOnly.FromDateTime(x.CreatedTime.Date) >= chartFromDate) &&
                    (DateOnly.FromDateTime(x.CreatedTime.Date) <= chartToDate))
                .ToList();

            var transactionChart = filteredTransactions
                .GroupBy(x => DateOnly.FromDateTime(x.CreatedTime.Date))
                .Select(g => new TransactionChartData
                {
                    Date = g.Key.ToDateTime(TimeOnly.MinValue),
                    TransactionCount = g.Count(),
                    Revenue = g.Where(x => x.Status == GeneralEnum.PaymentStatus.PAID)
                               .Sum(x => (decimal?)x.Amount ?? 0m)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return new TransactionStatisticsModel
            {
                TotalTransaction = totalTransaction,
                TotalRevenue = totalRevenue,
                PaidTransaction = paidTransaction,
                UnpaidTransaction = unpaidTransaction,
                RefundedTransaction = refundedTransaction,
                TransactionChart = transactionChart
            };
        }
    }
}

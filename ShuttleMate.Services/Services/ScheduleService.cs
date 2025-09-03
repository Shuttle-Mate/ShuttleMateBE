using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.Enum;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.ScheduleModelViews;
using ShuttleMate.ModelViews.ScheduleOverrideModelView;
using ShuttleMate.ModelViews.TripModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System.Globalization;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IStopEstimateService _stopEstimateService;
        private readonly INotificationService _notificationService;
        private readonly IGenericRepository<Schedule> _scheduleRepo;
        private readonly IGenericRepository<ScheduleOverride> _scheduleOverrideRepo;
        private readonly IGenericRepository<StopEstimate> _stopEstimateRepo;
        private readonly IGenericRepository<Route> _routeRepo;

        public ScheduleService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IStopEstimateService stopEstimateService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _stopEstimateService = stopEstimateService;
            _notificationService = notificationService;
            _scheduleRepo = _unitOfWork.GetRepository<Schedule>();
            _scheduleOverrideRepo = _unitOfWork.GetRepository<ScheduleOverride>();
            _stopEstimateRepo = _unitOfWork.GetRepository<StopEstimate>();
            _routeRepo = _unitOfWork.GetRepository<Route>();
        }

        public async Task<BasePaginatedList<ResponseScheduleModel>> GetAllByRouteIdAsync(
            Guid routeId,
            string from,
            string to,
            string? dayOfWeek,
            string? direction,
            bool sortAsc = true,
            int page = 0,
            int pageSize = 10)
        {
            if (!DateOnly.TryParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {from} không hợp lệ.");

            if (!DateOnly.TryParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {to} không hợp lệ.");

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(routeId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            var query = _unitOfWork.GetRepository<Schedule>()
                .GetQueryable()
                .Include(x => x.Shuttle)
                .Include(x => x.Driver)
                .Include(x => x.SchoolShift)
                .Where(x =>
                    x.RouteId == routeId &&
                    !x.DeletedTime.HasValue &&
                    x.From <= toDate &&
                    x.To >= fromDate
                );

            if (!string.IsNullOrWhiteSpace(direction) &&
                Enum.TryParse<RouteDirectionEnum>(direction, true, out var parsedDirection))
            {
                query = query.Where(x => x.Direction == parsedDirection);
            }

            query = sortAsc
                ? query.OrderBy(x => x.DepartureTime)
                : query.OrderByDescending(x => x.DepartureTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var scheduleIds = pagedItems.Select(x => x.Id).ToList();
            var overrideSchedules = await _unitOfWork.GetRepository<ScheduleOverride>()
                .GetQueryable()
                .Include(x => x.OverrideShuttle)
                .Include(x => x.OverrideUser)
                .Where(x => scheduleIds.Contains(x.ScheduleId) &&
                            x.Date >= fromDate &&
                            x.Date <= toDate &&
                            !x.DeletedTime.HasValue)
                .ToListAsync();

            var dayNamesFull = new[] { "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY" };

            var validDayIndexes = Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
                .Select(offset => fromDate.AddDays(offset))
                .Select(d => ((int)d.DayOfWeek + 6) % 7)
                .Distinct()
                .ToHashSet();

            var groupedQuery = pagedItems
                .SelectMany(schedule =>
                {
                    var scheduleOverrides = overrideSchedules.Where(x => x.ScheduleId == schedule.Id).ToList();

                    return schedule.DayOfWeek
                        .Select((c, idx) => new { c, idx })
                        .Where(d => d.c == '1')
                        .SelectMany(d =>
                        {
                            var matchingDates = Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
                                .Select(offset => fromDate.AddDays(offset))
                                .Where(date => ((int)date.DayOfWeek + 6) % 7 == d.idx)
                                .Where(date => date >= schedule.From && date <= schedule.To)
                                .ToList();

                            return matchingDates.Select(date =>
                            {
                                var scheduleDetail = _mapper.Map<ResponseScheduleDetailModel>(schedule);

                                var overrideForDate = scheduleOverrides.FirstOrDefault(x => x.Date == date);
                                if (overrideForDate != null)
                                {
                                    scheduleDetail.OverrideSchedule = new ResponseScheduleOverrideModel
                                    {
                                        Id = overrideForDate.Id,
                                        ShuttleReason = overrideForDate.ShuttleReason,
                                        DriverReason = overrideForDate.DriverReason,
                                        OverrideShuttle = overrideForDate.OverrideShuttleId.HasValue ?
                                            new ResponseShuttleScheduleModel
                                            {
                                                Id = overrideForDate.OverrideShuttleId.Value,
                                                Name = overrideForDate.OverrideShuttle?.Name ?? string.Empty
                                            } : null,
                                        OverrideDriver = overrideForDate.OverrideUserId.HasValue ?
                                            new ResponseDriverScheduleModel
                                            {
                                                Id = overrideForDate.OverrideUserId.Value,
                                                FullName = overrideForDate.OverrideUser?.FullName ?? string.Empty
                                            } : null
                                    };
                                }

                                return new
                                {
                                    DayName = dayNamesFull[d.idx],
                                    DayIndex = d.idx,
                                    Date = date.ToString("dd-MM-yyyy"),
                                    ScheduleDetail = scheduleDetail
                                };
                            });
                        });
                });

            if (!string.IsNullOrWhiteSpace(dayOfWeek))
            {
                var upperDay = dayOfWeek.ToUpperInvariant();
                var dayIndex = Array.IndexOf(dayNamesFull, upperDay);

                groupedQuery = groupedQuery.Where(x =>
                    x.DayName == upperDay &&
                    validDayIndexes.Contains(x.DayIndex));
            }
            else
            {
                groupedQuery = groupedQuery.Where(x => validDayIndexes.Contains(x.DayIndex));
            }

            var grouped = groupedQuery
                .GroupBy(x => new { x.DayName, x.Date })
                .Select(g => new ResponseScheduleModel
                {
                    DayOfWeek = g.Key.DayName,
                    Date = g.Key.Date,
                    Schedules = g.Select(x => x.ScheduleDetail).ToList()
                })
                .OrderBy(g => DateOnly.ParseExact(g.Date, "dd-MM-yyyy", CultureInfo.InvariantCulture))
                .ToList();

            return new BasePaginatedList<ResponseScheduleModel>(grouped, totalCount, page, pageSize);
        }

        public async Task<BasePaginatedList<ResponseScheduleModel>> GetAllByDriverIdAsync(
            Guid driverId,
            string from,
            string to,
            string? dayOfWeek,
            string? direction,
            bool sortAsc,
            int page = 0,
            int pageSize = 10)
        {
            if (!DateOnly.TryParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {from} không hợp lệ.");

            if (!DateOnly.TryParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {to} không hợp lệ.");

            var driver = await _unitOfWork.GetRepository<User>().GetByIdAsync(driverId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tài xế không tồn tại.");

            var query = _unitOfWork.GetRepository<Schedule>()
                .GetQueryable()
                .Include(x => x.Shuttle)
                .Include(x => x.Driver)
                .Include(x => x.SchoolShift)
                .Include(x => x.ScheduleOverrides)
                .Where(x => !x.DeletedTime.HasValue &&
                            x.From <= toDate &&
                            x.To >= fromDate);

            // Lấy lịch trình gốc và lịch trình được thay thế cho tài xế
            query = query.Where(f => f.DriverId == driverId || f.ScheduleOverrides.Any(o => o.OverrideUserId == driverId));

            if (!string.IsNullOrWhiteSpace(direction) && Enum.TryParse<RouteDirectionEnum>(direction, true, out var parsedDirection))
                query = query.Where(x => x.Direction == parsedDirection);

            query = sortAsc
                ? query.OrderBy(x => x.DepartureTime)
                : query.OrderByDescending(x => x.DepartureTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var scheduleIds = pagedItems.Select(x => x.Id).ToList();

            // Lấy tất cả lịch trình thay thế trong khoảng thời gian
            var overrideSchedules = await _unitOfWork.GetRepository<ScheduleOverride>()
                .GetQueryable()
                .Include(x => x.OverrideShuttle)
                .Include(x => x.OverrideUser)
                .Where(x => scheduleIds.Contains(x.ScheduleId) &&
                            x.Date >= fromDate &&
                            x.Date <= toDate &&
                            !x.DeletedTime.HasValue)
                .ToListAsync();

            var dayNamesFull = new[] { "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY" };

            var groupedQuery = pagedItems
                .SelectMany(schedule =>
                {
                    var scheduleOverrides = overrideSchedules.Where(x => x.ScheduleId == schedule.Id).ToList();

                    return schedule.DayOfWeek
                        .Select((c, idx) => new { c, idx })
                        .Where(d => d.c == '1')
                        .SelectMany(d =>
                        {
                            var matchingDates = Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
                                .Select(offset => fromDate.AddDays(offset))
                                .Where(date => ((int)date.DayOfWeek + 6) % 7 == d.idx)
                                .Where(date => date >= schedule.From && date <= schedule.To)
                                .ToList();

                            return matchingDates.Select(date =>
                            {
                                // Kiểm tra override cho ngày cụ thể
                                var overrideForDate = scheduleOverrides.FirstOrDefault(x => x.Date == date);

                                // Xác định xem schedule này có hiển thị cho driver hiện tại trong ngày này không
                                bool shouldDisplay = false;
                                ResponseScheduleDetailModel scheduleDetail = null;

                                // Trường hợp 1: Schedule gốc thuộc về driver hiện tại
                                if (schedule.DriverId == driverId)
                                {
                                    // Nếu có override cho ngày này và override cho driver khác -> không hiển thị
                                    if (overrideForDate != null && overrideForDate.OverrideUserId != null &&
                                        overrideForDate.OverrideUserId != driverId)
                                    {
                                        shouldDisplay = false;
                                    }
                                    else
                                    {
                                        // Không có override hoặc override vẫn cho driver hiện tại -> hiển thị
                                        shouldDisplay = true;
                                        scheduleDetail = _mapper.Map<ResponseScheduleDetailModel>(schedule);
                                    }
                                }
                                // Trường hợp 2: Schedule gốc thuộc driver khác nhưng được override cho driver hiện tại
                                else if (overrideForDate != null && overrideForDate.OverrideUserId == driverId)
                                {
                                    shouldDisplay = true;
                                    scheduleDetail = _mapper.Map<ResponseScheduleDetailModel>(schedule);

                                    // Cập nhật thông tin override
                                    scheduleDetail.Driver = new ResponseDriverScheduleModel
                                    {
                                        Id = driverId,
                                        FullName = driver.FullName
                                    };

                                    if (overrideForDate.OverrideShuttleId.HasValue)
                                    {
                                        scheduleDetail.Shuttle = new ResponseShuttleScheduleModel
                                        {
                                            Id = overrideForDate.OverrideShuttleId.Value,
                                            Name = overrideForDate.OverrideShuttle?.Name ?? string.Empty
                                        };
                                    }
                                }

                                if (shouldDisplay && scheduleDetail != null)
                                {
                                    // Thêm thông tin override nếu có
                                    if (overrideForDate != null)
                                    {
                                        scheduleDetail.OverrideSchedule = new ResponseScheduleOverrideModel
                                        {
                                            Id = overrideForDate.Id,
                                            ShuttleReason = overrideForDate.ShuttleReason,
                                            DriverReason = overrideForDate.DriverReason,
                                            OverrideShuttle = overrideForDate.OverrideShuttleId.HasValue ?
                                                new ResponseShuttleScheduleModel
                                                {
                                                    Id = overrideForDate.OverrideShuttleId.Value,
                                                    Name = overrideForDate.OverrideShuttle?.Name ?? string.Empty
                                                } : null,
                                            OverrideDriver = overrideForDate.OverrideUserId.HasValue ?
                                                new ResponseDriverScheduleModel
                                                {
                                                    Id = overrideForDate.OverrideUserId.Value,
                                                    FullName = overrideForDate.OverrideUser?.FullName ?? string.Empty
                                                } : null
                                        };
                                    }

                                    return new
                                    {
                                        DayName = dayNamesFull[d.idx],
                                        DayIndex = d.idx,
                                        Date = date.ToString("dd-MM-yyyy"),
                                        ScheduleDetail = scheduleDetail
                                    };
                                }

                                return null;
                            }).Where(x => x != null);
                        });
                });

            // Lọc theo dayOfWeek nếu có
            if (!string.IsNullOrWhiteSpace(dayOfWeek))
            {
                var upperDay = dayOfWeek.ToUpperInvariant();
                groupedQuery = groupedQuery.Where(x => x.DayName == upperDay);
            }

            var grouped = groupedQuery
                .GroupBy(x => new { x.DayName, x.Date })
                .Select(g => new ResponseScheduleModel
                {
                    DayOfWeek = g.Key.DayName,
                    Date = g.Key.Date,
                    Schedules = g.Select(x => x.ScheduleDetail).ToList()
                })
                .OrderBy(g => DateOnly.ParseExact(g.Date, "dd-MM-yyyy", CultureInfo.InvariantCulture))
                .ToList();

            return new BasePaginatedList<ResponseScheduleModel>(grouped, totalCount, page, pageSize);
        }

        public async Task<BasePaginatedList<ResponseOldScheduleModel>> GetAllAsync(
            Guid routeId,
            string from,
            string to,
            string? direction,
            bool sortAsc = true,
            int page = 0,
            int pageSize = 10)
        {
            if (!DateOnly.TryParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {from} không hợp lệ.");

            if (!DateOnly.TryParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {to} không hợp lệ.");

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(routeId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            var query = _unitOfWork.GetRepository<Schedule>()
                .GetQueryable()
                .Include(x => x.Shuttle)
                .Include(x => x.Driver)
                .Include(x => x.SchoolShift)
                .Where(x => x.RouteId == routeId && !x.DeletedTime.HasValue && x.From <= toDate &&
                    x.To >= fromDate);

            if (!string.IsNullOrWhiteSpace(direction) && Enum.TryParse<RouteDirectionEnum>(direction, true, out var parsedDirection))
            {
                query = query.Where(x => x.Direction == parsedDirection);
            }

            query = sortAsc
                ? query.OrderBy(x => x.DepartureTime)
                : query.OrderByDescending(x => x.DepartureTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ResponseOldScheduleModel>>(pagedItems);

            return new BasePaginatedList<ResponseOldScheduleModel>(result, totalCount, page, pageSize);
        }

        public async Task<IEnumerable<ResponseTodayScheduleForDriverModel>> GetAllTodayAsync()
        {
            var driverId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(driverId, out Guid driverIdGuid);
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);
            var dayOfWeek = (int)vietnamNow.DayOfWeek;

            var overrides = await _scheduleOverrideRepo.Entities
                .Where(o => o.Date == todayVN && !o.DeletedTime.HasValue && o.OverrideUserId == driverIdGuid)
                .ToListAsync();

            var overriddenScheduleIds = overrides
                .Select(o => o.ScheduleId)
                .ToList();

            var schedules = await _scheduleRepo.Entities
                .Where(s => s.DriverId == driverIdGuid
                    && !s.DeletedTime.HasValue
                    && s.From <= todayVN
                    && s.To >= todayVN
                    && !overriddenScheduleIds.Contains(s.Id))
                .Include(s => s.Shuttle)
                .Include(s => s.Driver)
                .Include(s => s.SchoolShift)
                .Include(s => s.Route)
                    .ThenInclude(r => r.RouteStops)
                        .ThenInclude(rs => rs.Stop)
                .ToListAsync();

            var responseList = new List<ResponseTodayScheduleForDriverModel>();

            foreach (var s in schedules)
            {
                if (!IsBitSet(s.DayOfWeek, dayOfWeek)) continue;

                var stopEstimates = await _unitOfWork.GetRepository<StopEstimate>().Entities
                    .Where(se => se.ScheduleId == s.Id)
                    .OrderBy(se => s.Direction == RouteDirectionEnum.IN_BOUND
                        ? se.Stop.RouteStops.FirstOrDefault(rs => rs.RouteId == s.RouteId).StopOrder
                        : -se.Stop.RouteStops.FirstOrDefault(rs => rs.RouteId == s.RouteId).StopOrder)
                    .ToListAsync();

                if (!stopEstimates.Any()) continue;

                var startTime = stopEstimates.First().ExpectedTime;
                var endTime = stopEstimates.Last().ExpectedTime;
                var duration = endTime - startTime;

                var routeStops = s.Route?.RouteStops?.Where(x => !x.DeletedTime.HasValue).ToList() ?? new();
                var routeInfo = BuildRouteInfo(routeStops, s.Direction);

                var expectedStudentIds = await _unitOfWork.GetRepository<User>().Entities
                    .Where(st => st.HistoryTickets.Any(ht =>
                        ht.Ticket.RouteId == s.RouteId &&
                        ht.Ticket.Route.IsActive == true &&
                        ht.ValidUntil >= todayVN &&
                        ht.ValidFrom <= todayVN &&
                        ht.HistoryTicketSchoolShifts.Any(hs => hs.SchoolShiftId == s.SchoolShiftId) &&
                        ht.Status == HistoryTicketStatus.PAID &&
                        !ht.DeletedTime.HasValue))
                    .Select(st => st.Id)
                    .ToListAsync();

                var attendedCount = await _unitOfWork.GetRepository<Attendance>().Entities
                    .Where(a =>
                        expectedStudentIds.Contains(a.HistoryTicket.UserId) &&
                        a.Trip.TripDate == todayVN &&
                        a.Trip.Schedule.RouteId == s.RouteId &&
                        a.Trip.Schedule.SchoolShiftId == s.SchoolShiftId &&
                        (a.Status == AttendanceStatusEnum.CHECKED_IN || a.Status == AttendanceStatusEnum.CHECKED_OUT)).Select(a => a.HistoryTicket.UserId)
                    .Distinct()
                    .CountAsync();

                var shuttle = s.Shuttle;

                var trip = await _unitOfWork.GetRepository<Trip>()
                    .Entities
                    .FirstOrDefaultAsync(t => t.ScheduleId == s.Id && t.TripDate == todayVN);

                var tripMapped = _mapper.Map<ResponseTripModel>(trip);

                responseList.Add(new ResponseTodayScheduleForDriverModel
                {
                    Id = s.Id,
                    RouteId = s.RouteId,
                    SchoolShiftId = (Guid)s.SchoolShiftId,
                    Trip = tripMapped,
                    RouteCode = s.Route?.RouteCode ?? "",
                    RouteName = s.Route?.RouteName ?? "",
                    StartTime = startTime.ToString("HH:mm"),
                    EndTime = endTime.ToString("HH:mm"),
                    ShuttleName = shuttle?.Name ?? "",
                    LicensePlate = shuttle?.LicensePlate ?? "",
                    VehicleType = shuttle?.VehicleType ?? "",
                    Color = shuttle?.Color ?? "",
                    SeatCount = shuttle?.SeatCount ?? 0,
                    Brand = shuttle?.Brand ?? "",
                    Model = shuttle?.Model ?? "",
                    RegistrationDate = shuttle?.RegistrationDate ?? DateTime.MinValue,
                    InspectionDate = shuttle?.InspectionDate ?? DateTime.MinValue,
                    NextInspectionDate = shuttle?.NextInspectionDate ?? DateTime.MinValue,
                    InsuranceExpiryDate = shuttle?.InsuranceExpiryDate ?? DateTime.MinValue,
                    AttendedStudentCount = attendedCount,
                    ExpectedStudentCount = expectedStudentIds.Count,
                    EstimatedDuration = $"{(int)duration.TotalHours}h{duration.Minutes}m",
                    Direction = s.Direction.ToString(),
                    Route = routeInfo,
                    TripStatus = s.Trips.FirstOrDefault(t => t.TripDate == todayVN && !t.DeletedTime.HasValue && t.ScheduleId == s.Id)?.Status.ToString() ?? $"{TripStatusEnum.SCHEDULED.ToString()}"
                });
            }

            foreach (var o in overrides)
            {
                var s = o.Schedule;
                var startTime = s.DepartureTime;
                var endTime = startTime.AddHours(1);
                var duration = endTime - startTime;

                var routeStops = s.Route?.RouteStops?.Where(x => !x.DeletedTime.HasValue).ToList() ?? new();
                var routeInfo = BuildRouteInfo(routeStops, s.Direction);

                var expectedStudentIds = await _unitOfWork.GetRepository<UserSchoolShift>().Entities
                    .Where(us => us.SchoolShiftId == s.SchoolShiftId)
                    .Select(us => us.StudentId)
                    .ToListAsync();

                var attendedCount = await _unitOfWork.GetRepository<Attendance>().Entities
                    .Where(a =>
                        expectedStudentIds.Contains(a.HistoryTicket.UserId) &&
                        a.Trip.TripDate == todayVN &&
                        a.Trip.Schedule.RouteId == s.RouteId &&
                        a.Trip.Schedule.SchoolShiftId == s.SchoolShiftId &&
                        (a.Status == AttendanceStatusEnum.CHECKED_IN || a.Status == AttendanceStatusEnum.CHECKED_OUT))
                    .Select(a => a.HistoryTicket.UserId)
                    .Distinct()
                    .CountAsync();

                var shuttle = o.OverrideShuttle;

                var trip = await _unitOfWork.GetRepository<Trip>()
                    .Entities
                    .FirstOrDefaultAsync(t => t.ScheduleId == s.Id);

                var tripMapped = _mapper.Map<ResponseTripModel>(trip);

                responseList.Add(new ResponseTodayScheduleForDriverModel
                {
                    Id = s.Id,
                    RouteId = s.RouteId,
                    Trip = tripMapped,
                    SchoolShiftId = (Guid)s.SchoolShiftId,
                    RouteCode = s.Route?.RouteCode ?? "",
                    RouteName = s.Route?.RouteName ?? "",
                    StartTime = startTime.ToString("HH:mm"),
                    EndTime = endTime.ToString("HH:mm"),
                    ShuttleName = shuttle?.Name ?? "",
                    LicensePlate = shuttle?.LicensePlate ?? "",
                    VehicleType = shuttle?.VehicleType ?? "",
                    Color = shuttle?.Color ?? "",
                    SeatCount = shuttle?.SeatCount ?? 0,
                    Brand = shuttle?.Brand ?? "",
                    Model = shuttle?.Model ?? "",
                    RegistrationDate = shuttle?.RegistrationDate ?? DateTime.MinValue,
                    InspectionDate = shuttle?.InspectionDate ?? DateTime.MinValue,
                    NextInspectionDate = shuttle?.NextInspectionDate ?? DateTime.MinValue,
                    InsuranceExpiryDate = shuttle?.InsuranceExpiryDate ?? DateTime.MinValue,
                    AttendedStudentCount = attendedCount,
                    ExpectedStudentCount = expectedStudentIds.Count,
                    EstimatedDuration = $"{(int)duration.TotalHours}h{duration.Minutes}m",
                    Direction = s.Direction.ToString(),
                    Route = routeInfo,
                    TripStatus = s.Trips.FirstOrDefault(t => t.TripDate == todayVN && !t.DeletedTime.HasValue && t.ScheduleId == s.Id)?.Status.ToString() ?? $"{TripStatusEnum.SCHEDULED.ToString()}"
                });
            }

            return responseList.OrderBy(x => TimeOnly.Parse(x.StartTime));
        }

        public async Task<ResponseScheduleModel> GetByIdAsync(Guid scheduleId)
        {
            var schedule = await _scheduleRepo.GetQueryable()
                .Include(x => x.Shuttle)
                .Include(x => x.Driver)
                .Include(x => x.SchoolShift)
                .Where(f => f.Id == scheduleId && !f.DeletedTime.HasValue)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không tồn tại.");

            var overrideSchedules = await _scheduleOverrideRepo.GetQueryable()
                .Include(x => x.OverrideShuttle)
                .Include(x => x.OverrideUser)
                .Where(x => x.ScheduleId == scheduleId &&
                            x.Date >= schedule.From &&
                            x.Date <= schedule.To &&
                            !x.DeletedTime.HasValue)
                .AsNoTracking()
                .ToListAsync();

            var dayNamesFull = new[] { "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY" };

            var scheduleDetails = schedule.DayOfWeek
                .Select((c, idx) => new { c, idx })
                .Where(d => d.c == '1')
                .SelectMany(d =>
                {
                    var matchingDates = Enumerable.Range(0, schedule.To.DayNumber - schedule.From.DayNumber + 1)
                        .Select(offset => schedule.From.AddDays(offset))
                        .Where(date => ((int)date.DayOfWeek + 6) % 7 == d.idx)
                        .Where(date => date >= schedule.From && date <= schedule.To)
                        .ToList();

            return matchingDates.Select(date =>
            {
                var scheduleDetail = _mapper.Map<ResponseScheduleDetailModel>(schedule);

                var overrideForDate = overrideSchedules.FirstOrDefault(x => x.Date == date);
                if (overrideForDate != null)
                {
                    scheduleDetail.OverrideSchedule = new ResponseScheduleOverrideModel
                    {
                        Id = overrideForDate.Id,
                        ShuttleReason = overrideForDate.ShuttleReason,
                        DriverReason = overrideForDate.DriverReason,
                        OverrideShuttle = overrideForDate.OverrideShuttleId.HasValue ?
                            new ResponseShuttleScheduleModel
                            {
                                Id = overrideForDate.OverrideShuttleId.Value,
                                Name = overrideForDate.OverrideShuttle?.Name ?? string.Empty
                            } : null,
                        OverrideDriver = overrideForDate.OverrideUserId.HasValue ?
                            new ResponseDriverScheduleModel
                            {
                                Id = overrideForDate.OverrideUserId.Value,
                                FullName = overrideForDate.OverrideUser?.FullName ?? string.Empty
                            } : null
                    };
                }

                return new
                {
                    DayName = dayNamesFull[d.idx],
                    DayIndex = d.idx,
                    Date = date.ToString("dd-MM-yyyy"),
                    ScheduleDetail = scheduleDetail
                };
            });
        })
        .GroupBy(x => new { x.DayName, x.Date })
        .Select(g => new ResponseScheduleModel
        {
            DayOfWeek = g.Key.DayName,
            Date = g.Key.Date,
            Schedules = g.Select(x => x.ScheduleDetail).ToList()
        })
        .OrderBy(g => DateOnly.ParseExact(g.Date, "dd-MM-yyyy", CultureInfo.InvariantCulture))
        .FirstOrDefault();

            return scheduleDetails;
        }

        public async Task CreateAsync(CreateScheduleModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);

            // Xác định ngày bắt đầu và kết thúc của tuần tiếp theo dựa vào ngày hiện tại
            var daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)todayVN.DayOfWeek + 7) % 7;
            var nextWeekStart = todayVN.AddDays(daysUntilNextMonday == 0 ? 7 : daysUntilNextMonday);
            var nextWeekEnd = nextWeekStart.AddDays(6);

            // Nếu tạo cho tuần tiếp theo, kiểm tra và xóa lịch trình cũ nếu tồn tại
            //if (model.From == nextWeekStart && model.To == nextWeekEnd)
            //{
            //    var existingSchedules = await _scheduleRepo.FindAllAsync(x =>
            //        x.RouteId == model.RouteId &&
            //        !x.DeletedTime.HasValue &&
            //        x.From == model.From &&
            //        x.To == model.To);

            //    if (existingSchedules.Any())
            //    {
            //        var scheduleIds = existingSchedules.Select(x => x.Id).ToList();
            //        var existingStopEstimates = await _stopEstimateRepo.FindAllAsync(x => scheduleIds.Contains(x.ScheduleId));

            //        if (existingStopEstimates.Any())
            //            await _stopEstimateRepo.DeleteRangeAsync(existingStopEstimates);

            //        await _scheduleRepo.DeleteRangeAsync(existingSchedules);
            //        await _unitOfWork.SaveAsync();
            //    }
            //}

            var route = await _routeRepo.GetByIdAsync(model.RouteId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            if (model.Schedules == null || !model.Schedules.Any())
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Danh sách lịch trình không được để trống.");
            
            if (model.From > model.To)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày bắt đầu không được lớn hơn ngày kết thúc.");

            if ((model.To.DayNumber - model.From.DayNumber) != 6)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày bắt đầu và ngày kết thú phải cách nhau đúng 7 ngày (1 tuần).");

            var newSchedules = new List<Schedule>();

            foreach (var scheduleDetail in model.Schedules)
            {
                var shuttle = await _unitOfWork.GetRepository<Shuttle>().GetByIdAsync(scheduleDetail.ShuttleId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, $"Xe với Id {scheduleDetail.ShuttleId} không tồn tại.");

                if (shuttle.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {shuttle.Name} đã bị xóa.");

                if (!shuttle.IsActive)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {shuttle.Name} trong trạng thái không hoạt động.");

                var driver = await _unitOfWork.GetRepository<User>().GetByIdAsync(scheduleDetail.DriverId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, $"Tài xế với Id {scheduleDetail.DriverId} không tồn tại.");

                if (driver.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {driver.FullName} đã bị xóa.");

                if (driver.Violate == true)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {driver.FullName} đã bị khóa.");

                var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().GetByIdAsync(scheduleDetail.SchoolShiftId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, $"Ca học với Id {scheduleDetail.SchoolShiftId} không tồn tại.");

                if (schoolShift.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ca học {GetSchoolShiftDescription(schoolShift)} đã bị xóa.");

                if (schoolShift.SchoolId != route.SchoolId)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ca học {GetSchoolShiftDescription(schoolShift)} không thuộc về trường của tuyến này.");

                if (scheduleDetail.DayOfWeeks == null || !scheduleDetail.DayOfWeeks.Any())
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Danh sách ngày trong tuần không được để trống.");

                var existingSchedules = await _scheduleRepo.Entities
                    .Include(x => x.ScheduleOverrides)
                    .Include(x => x.SchoolShift)
                    .Where(x => (x.ShuttleId == scheduleDetail.ShuttleId || x.DriverId == scheduleDetail.DriverId) &&
                                !x.DeletedTime.HasValue &&
                                model.From <= x.To &&
                                model.To >= x.From &&
                                x.SchoolShift.ShiftType == schoolShift.ShiftType &&
                                x.SchoolShift.SessionType == schoolShift.SessionType)
                    .ToListAsync();

                var timeStr = scheduleDetail.DepartureTime;

                if (!TimeOnly.TryParse(timeStr, out var time))
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành không hợp lệ: {timeStr}.");

                var routeStops = route.RouteStops?.Where(rs => !rs.DeletedTime.HasValue).ToList();
                var stopCount = routeStops?.Count ?? 0;
                int totalDuration = 0;
                
                if (stopCount > 1)
                {
                    var durationSum = routeStops.Sum(rs => rs.Duration);
                    totalDuration = durationSum + 300 * (stopCount - 1);
                }

                if (schoolShift.ShiftType == ShiftTypeEnum.START)
                {
                    if (time > schoolShift.Time)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành {timeStr} phải nhỏ hơn hoặc bằng giờ bắt đầu của ca ({GetSchoolShiftDescription(schoolShift)} lúc {schoolShift.Time}).");

                    var timeDiffInSeconds = (schoolShift.Time.ToTimeSpan() - time.ToTimeSpan()).TotalSeconds;
                    if (timeDiffInSeconds < totalDuration)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành {timeStr} phải cách giờ bắt đầu ca ít nhất {totalDuration / 60} phút để kịp di chuyển.");
                }

                else if (schoolShift.ShiftType == ShiftTypeEnum.END && time < schoolShift.Time)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành {timeStr} phải lớn hơn hoặc bằng giờ kết thúc ca ({GetSchoolShiftDescription(schoolShift)} lúc {schoolShift.Time}).");

                var direction = schoolShift.ShiftType == ShiftTypeEnum.START
                    ? GeneralEnum.RouteDirectionEnum.IN_BOUND
                    : GeneralEnum.RouteDirectionEnum.OUT_BOUND;
                var days = scheduleDetail.DayOfWeeks.Select(d => d.DayOfWeek.Trim().ToUpper()).ToList();
                var binaryDayOfWeek = ConvertToBinaryDayOfWeek(days);

                foreach (var day in days)
                {
                    var dayIndex = ConvertDayOfWeekToIndex(day);
                    var currentDate = GetDateForDayOfWeek(model.From, day);

                    foreach (var existing in existingSchedules)
                    {
                        var overrideForThisDate = existing.ScheduleOverrides?
                            .FirstOrDefault(o => o.Date == currentDate && !o.DeletedTime.HasValue);

                        Guid actualDriverId = overrideForThisDate?.OverrideUserId ?? existing.DriverId;
                        Guid actualShuttleId = overrideForThisDate?.OverrideShuttleId ?? existing.ShuttleId;

                        if (actualDriverId == scheduleDetail.DriverId &&
                            existing.SchoolShift.ShiftType == schoolShift.ShiftType &&
                            existing.SchoolShift.SessionType == schoolShift.SessionType &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1' &&
                            model.From <= existing.To &&
                            model.To >= existing.From)
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế {driver.FullName} đã được phân công ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime} (từ {existing.From:dd/MM/yyyy} đến {existing.To:dd/MM/yyyy}).");

                        if (actualShuttleId == scheduleDetail.ShuttleId &&
                            existing.SchoolShift.ShiftType == schoolShift.ShiftType &&
                            existing.SchoolShift.SessionType == schoolShift.SessionType &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1' &&
                            model.From <= existing.To &&
                            model.To >= existing.From)
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe {shuttle.Name} đã được phân công ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime} (từ {existing.From:dd/MM/yyyy} đến {existing.To:dd/MM/yyyy}).");
                    }

                    foreach (var newItem in newSchedules)
                    {
                        if (newItem.DriverId == scheduleDetail.DriverId &&
                            newItem.SchoolShiftId == schoolShift.Id &&
                            newItem.Direction == direction &&
                            newItem.DayOfWeek[dayIndex] == '1')
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế {driver.FullName} đang được tạo mới ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} bị trùng với một ca khác trong cùng đợt tạo.");

                        if (newItem.ShuttleId == scheduleDetail.ShuttleId &&
                            newItem.SchoolShiftId == schoolShift.Id &&
                            newItem.Direction == direction &&
                            newItem.DayOfWeek[dayIndex] == '1')
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe {shuttle.Name} đang được tạo mới ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} bị trùng với một ca khác trong cùng đợt tạo.");
                    }
                }

                var existingRouteSchedules = existingSchedules.Where(x => x.RouteId == route.Id);

                foreach (var existing in existingRouteSchedules)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (binaryDayOfWeek[i] == '1' && existing.DayOfWeek[i] == '1')
                        {
                            var currentDate = model.From.AddDays(i);
                            var overrideForThisDate = existing.ScheduleOverrides?
                                .FirstOrDefault(o => o.Date == currentDate && !o.DeletedTime.HasValue);

                            // Nếu có override, không kiểm tra trùng giờ vì đã được override
                            if (overrideForThisDate == null)
                            {
                                var timeDiff = Math.Abs((time.ToTimeSpan() - existing.DepartureTime.ToTimeSpan()).TotalMinutes);
                                if (timeDiff < 10)
                                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                        $"Giờ khởi hành {timeStr} đã tồn tại trong cùng một thứ với khoảng cách nhỏ hơn 10 phút.");
                            }
                        }
                    }
                }

                foreach (var newItem in newSchedules)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (binaryDayOfWeek[i] == '1' && newItem.DayOfWeek[i] == '1')
                        {
                            var timeDiff = Math.Abs((time.ToTimeSpan() - newItem.DepartureTime.ToTimeSpan()).TotalMinutes);
                            if (timeDiff < 10)
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Trong dữ liệu tạo mới có giờ khởi hành {timeStr} trùng nhau trong cùng một thứ (<10 phút).");
                        }
                    }
                }

                newSchedules.Add(new Schedule
                {
                    RouteId = model.RouteId,
                    ShuttleId = scheduleDetail.ShuttleId,
                    DriverId = scheduleDetail.DriverId,
                    From = model.From,
                    To = model.To,
                    DepartureTime = time,
                    DayOfWeek = binaryDayOfWeek,
                    SchoolShiftId = schoolShift.Id,
                    Direction = direction,
                    CreatedBy = userId,
                    LastUpdatedBy = userId
                });
            }

            if (newSchedules.Any())
                await _scheduleRepo.InsertRangeAsync(newSchedules);

            var userIds = newSchedules.Select(x => x.Driver.Id).ToList();

            var users = await _unitOfWork.GetRepository<User>().Entities
                .Where(u => !u.DeletedTime.HasValue)
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName})
                .ToListAsync();

            var createdBy = "system";

            foreach (var user in users)
            {
                DateTime dateTime = vietnamNow;

                var metadata = new Dictionary<string, string>
                {
                    { "DriverName", user.FullName }
                };

                // Đẩy thông báo
                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "UpdateSchedule",
                    recipientIds: new List<Guid> { user.Id },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "SCHEDULE"
                );
            }

            await _stopEstimateService.CreateAsync(newSchedules, model.RouteId);

            // Cập nhật thời gian hoạt động và thời gian chạy của tuyến
            var allSchedules = await _scheduleRepo.FindAllAsync(s => s.RouteId == model.RouteId && !s.DeletedTime.HasValue);

            if (allSchedules != null && allSchedules.Any())
            {
                var minTime = allSchedules.Min(s => s.DepartureTime);
                var maxTime = allSchedules.Max(s => s.DepartureTime);

                route.OperatingTime = $"{minTime:HH\\:mm} - {maxTime:HH\\:mm}";

                var earliestSchedule = allSchedules.OrderBy(s => s.DepartureTime).FirstOrDefault();
                if (earliestSchedule != null)
                {
                    var stopEstimates = await _stopEstimateRepo.FindAllAsync(se =>
                        se.ScheduleId == earliestSchedule.Id);

                    if (stopEstimates != null && stopEstimates.Any())
                    {
                        var earliest = stopEstimates.Min(se => se.ExpectedTime);
                        var latest = stopEstimates.Max(se => se.ExpectedTime);
                        route.RunningTime = (latest - earliest).TotalSeconds.ToString();
                    }
                }

                route.LastUpdatedBy = userId;
                route.LastUpdatedTime = CoreHelper.SystemTimeNow;

                await _routeRepo.UpdateAsync(route);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid scheduleId, UpdateScheduleModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.DepartureTime = model.DepartureTime.Trim();

            var schedule = await _unitOfWork.GetRepository<Schedule>().GetByIdAsync(scheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không tồn tại.");

            var oldDriverId = schedule.DriverId;

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(schedule.RouteId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            var shuttle = await _unitOfWork.GetRepository<Shuttle>().GetByIdAsync(model.ShuttleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, $"Xe không tồn tại.");

            if (shuttle.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.NOT_FOUND, $"Xe {shuttle.Name} đã bị xóa.");

            if (!shuttle.IsActive)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {shuttle.Name} trong trạng thái không hoạt động.");

            var driver = await _unitOfWork.GetRepository<User>().GetByIdAsync(model.DriverId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, $"Tài xế không tồn tại.");

            if (driver.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {driver.FullName} đã bị xóa.");

            if (driver.Violate == true)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {driver.FullName} đã bị khóa.");

            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().GetByIdAsync(model.SchoolShiftId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, $"Ca học không tồn tại.");

            if (schoolShift.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ca học {GetSchoolShiftDescription(schoolShift)} đã bị xóa.");

            if (schoolShift.SchoolId != route.SchoolId)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ca học {GetSchoolShiftDescription(schoolShift)} không thuộc về trường của tuyến này.");

            if (!TimeOnly.TryParse(model.DepartureTime, out var time))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành không hợp lệ: {model.DepartureTime}.");

            var direction = schoolShift.ShiftType == ShiftTypeEnum.START
                ? GeneralEnum.RouteDirectionEnum.IN_BOUND
                : GeneralEnum.RouteDirectionEnum.OUT_BOUND;

            if (schoolShift.ShiftType == ShiftTypeEnum.START && time > schoolShift.Time)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                    $"Giờ khởi hành phải nhỏ hơn hoặc bằng giờ bắt đầu của ca ({GetSchoolShiftDescription(schoolShift)} lúc {schoolShift.Time}).");
            }

            if (schoolShift.ShiftType == ShiftTypeEnum.END && time < schoolShift.Time)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                    $"Giờ khởi hành phải lớn hơn hoặc bằng giờ kết thúc của ca ({GetSchoolShiftDescription(schoolShift)} lúc {schoolShift.Time}).");
            }

            var days = model.DayOfWeeks.Select(d => d.DayOfWeek.Trim().ToUpper()).ToList();
            var binaryDayOfWeek = ConvertToBinaryDayOfWeek(days);

            var existingSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                x.Id != scheduleId &&
                (x.DriverId == model.DriverId || x.ShuttleId == model.ShuttleId) &&
                !x.DeletedTime.HasValue &&
                schedule.From <= x.To &&
                schedule.To >= x.From);

            foreach (var day in days)
            {
                var dayIndex = ConvertDayOfWeekToIndex(day);

                foreach (var existing in existingSchedules.Where(x => x.DriverId == model.DriverId))
                {
                    if (existing.SchoolShiftId == schoolShift.Id &&
                        existing.Direction == direction &&
                        existing.DayOfWeek[dayIndex] == '1' &&
                        schedule.From <= existing.To &&
                        schedule.To >= existing.From)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Tài xế {driver.FullName} đã có ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime} (từ {existing.From:dd/MM/yyyy} đến {existing.To:dd/MM/yyyy}).");
                    }
                }

                foreach (var existing in existingSchedules.Where(x => x.ShuttleId == model.ShuttleId))
                {
                    if (existing.SchoolShiftId == schoolShift.Id &&
                        existing.Direction == direction &&
                        existing.DayOfWeek[dayIndex] == '1' &&
                        schedule.From <= existing.To &&
                        schedule.To >= existing.From)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe {shuttle.Name} đã có ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime} (từ {existing.From:dd/MM/yyyy} đến {existing.To:dd/MM/yyyy}).");
                    }
                }
            }

            var existingRouteSchedules = existingSchedules.Where(x => x.RouteId == route.Id);
            foreach (var existing in existingRouteSchedules)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (binaryDayOfWeek[i] == '1' && existing.DayOfWeek[i] == '1')
                    {
                        var timeDiff = Math.Abs((time.ToTimeSpan() - existing.DepartureTime.ToTimeSpan()).TotalMinutes);
                        if (timeDiff < 10)
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Có lịch trình khác với giờ khởi hành gần {model.DepartureTime} (chênh lệch {timeDiff} phút) trong cùng thứ.");
                        }
                    }
                }
            }

            schedule.ShuttleId = model.ShuttleId;
            schedule.DriverId = model.DriverId;
            schedule.SchoolShiftId = model.SchoolShiftId;
            schedule.DepartureTime = time;
            schedule.DayOfWeek = binaryDayOfWeek;
            schedule.Direction = direction;
            schedule.LastUpdatedBy = userId;
            schedule.LastUpdatedTime = DateTime.UtcNow;

            await _stopEstimateService.UpdateAsync(new List<Schedule> { schedule }, schedule.RouteId);
            await _unitOfWork.GetRepository<Schedule>().UpdateAsync(schedule);
            await _unitOfWork.SaveAsync();

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            DateTime dateTime = vietnamNow;

            // lấy thông tin tài xế mới
            var newDriverId = schedule.DriverId;

            // lấy thông tin tài xế cũ và mới (tránh gửi trùng nếu là cùng 1 người)
            var driverIds = new List<Guid> { oldDriverId };
            if (oldDriverId != newDriverId)
                driverIds.Add(newDriverId);

            foreach (var driverId in driverIds)
            {
                var driverTemp = await _unitOfWork.GetRepository<User>().GetByIdAsync(driverId);

                var metadata = new Dictionary<string, string>
                {
                    { "DriverName", driverTemp.FullName }
                };

                // đẩy thông báo tài xế
                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "UpdateSchedule",
                    recipientIds: new List<Guid> { driverId },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "SCHEDULE"
                );
            }
        }

        public async Task DeleteAsync(Guid scheduleId, string dayOfWeek)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var schedule = await _unitOfWork.GetRepository<Schedule>().GetByIdAsync(scheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không tồn tại.");

            // Lấy thông tin tài xế trước khi xóa hoặc cập nhật
            var driver = schedule.Driver;
            if (driver == null)
            {
                // Nếu navigation property chưa được load, load từ DB
                driver = await _unitOfWork.GetRepository<User>().GetByIdAsync(schedule.DriverId);
            }

            string driverName = driver?.FullName ?? "";
            Guid driverId = driver?.Id ?? Guid.Empty;

            int index = ConvertDayOfWeekToIndex(dayOfWeek);

            if (!IsBitSet(schedule.DayOfWeek, index))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Lịch trình không chứa ngày này.");

            bool hasTripsForDay = schedule.Trips != null &&
                schedule.Trips.Any(t =>
                    t.TripDate >= schedule.From &&
                    t.TripDate <= schedule.To &&
                    t.TripDate.DayOfWeek.ToString().Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase)
                );

            if (hasTripsForDay)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Không thể xóa lịch trình vì thứ này đã có chuyến đi.");

            var bits = schedule.DayOfWeek.ToCharArray();

            if (bits.Count(c => c == '1') == 1)
            {
                if (schedule.StopEstimates.Any() == true)
                    await _unitOfWork.GetRepository<StopEstimate>().DeleteRangeAsync(schedule.StopEstimates);

                await _unitOfWork.GetRepository<Schedule>().DeleteAsync(schedule);
            }
            else
            {
                bits[index] = '0';
                schedule.DayOfWeek = new string(bits);
                schedule.LastUpdatedTime = CoreHelper.SystemTimeNow;
                schedule.LastUpdatedBy = userId;

                await _unitOfWork.GetRepository<Schedule>().UpdateAsync(schedule);
            }

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            DateTime dateTime = vietnamNow;

            var metadata = new Dictionary<string, string>
            {
                { "DriverName", driverName }
            };

            if (driverId != Guid.Empty)
            {
                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "UpdateSchedule",
                    recipientIds: new List<Guid> { driverId },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "SCHEDULE"
                );
            }

            await _unitOfWork.SaveAsync();
        }

        #region Private Methods
        private DateOnly GetDateForDayOfWeek(DateOnly startDate, string dayOfWeek)
        {
            var dayIndex = ConvertDayOfWeekToIndex(dayOfWeek);
            return startDate.AddDays(dayIndex);
        }

        private string ConvertToBinaryDayOfWeek(IEnumerable<string> days)
        {
            var map = new Dictionary<string, int>
                {
                    { "MONDAY", 0 },
                    { "TUESDAY", 1 },
                    { "WEDNESDAY", 2 },
                    { "THURSDAY", 3 },
                    { "FRIDAY", 4 },
                    { "SATURDAY", 5 },
                    { "SUNDAY", 6 }
                };

            var binary = new char[7] { '0', '0', '0', '0', '0', '0', '0' };

            foreach (var day in days)
            {
                if (!map.TryGetValue(day, out var index))
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày không hợp lệ: {day}");
                }

                binary[index] = '1';
            }

            return new string(binary);
        }

        private int ConvertDayOfWeekToIndex(string day)
        {
            return day.ToLower() switch
            {
                "monday" => 0,
                "tuesday" => 1,
                "wednesday" => 2,
                "thursday" => 3,
                "friday" => 4,
                "saturday" => 5,
                "sunday" => 6,
                _ => throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Thứ không hợp lệ: {day}")
            };
        }

        private string ConvertDayOfWeekToVietnamese(string day)
        {
            return day.ToLower() switch
            {
                "monday" => "thứ hai",
                "tuesday" => "thứ ba",
                "wednesday" => "thứ tư",
                "thursday" => "thứ năm",
                "friday" => "thứ sáu",
                "saturday" => "thứ bảy",
                "sunday" => "chủ nhật",
                _ => throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Thứ không hợp lệ: {day}")
            };
        }

        private ResponseRouteScheduleForDriverModel BuildRouteInfo(List<RouteStop> routeStops, RouteDirectionEnum direction)
        {
            if (routeStops == null || routeStops.Count == 0)
                return new ResponseRouteScheduleForDriverModel { From = "", To = "", StopsCount = 0 };

            var orderedStops = direction == RouteDirectionEnum.IN_BOUND
                ? routeStops.OrderBy(rs => rs.StopOrder).ToList()
                : routeStops.OrderByDescending(rs => rs.StopOrder).ToList();

            return new ResponseRouteScheduleForDriverModel
            {
                From = orderedStops.First()?.Stop?.Name,
                To = orderedStops.Last()?.Stop?.Name,
                StopsCount = orderedStops.Count
            };
        }

        private bool IsBitSet(string binary, int position)
        {
            if (string.IsNullOrWhiteSpace(binary) || binary.Length != 7)
                return false;

            return binary[position] == '1';
        }

        private string GetSchoolShiftDescription(SchoolShift schoolShift)
        {
            string shiftType = schoolShift.ShiftType switch
            {
                ShiftTypeEnum.START => "vào học",
                ShiftTypeEnum.END => "tan học",
                _ => "Không xác định"
            };

            string sessionType = schoolShift.SessionType switch
            {
                SessionTypeEnum.MORNING => "sáng",
                SessionTypeEnum.AFTERNOON => "chiều",
                _ => "Không xác định"
            };

            return $"{shiftType} buổi {sessionType}";
        }

        #endregion
    }
}

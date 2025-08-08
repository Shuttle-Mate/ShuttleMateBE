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
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IStopEstimateService _stopEstimateService;

        public ScheduleService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IStopEstimateService stopEstimateService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _stopEstimateService = stopEstimateService;
        }

        public async Task<BasePaginatedList<ResponseScheduleModel>> GetAllByRouteIdAsync(
            Guid routeId,
            string? direction,
            bool sortAsc = true,
            int page = 0,
            int pageSize = 10)
        {
            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(routeId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            var query = _unitOfWork.GetRepository<Schedule>()
                .GetQueryable()
                .Include(x => x.Shuttle)
                .Include(x => x.Driver)
                .Include(x => x.SchoolShift)
                .Where(x => x.RouteId == routeId && !x.DeletedTime.HasValue);

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

            var result = _mapper.Map<List<ResponseScheduleModel>>(pagedItems);

            return new BasePaginatedList<ResponseScheduleModel>(result, totalCount, page, pageSize);
        }

        public async Task<IEnumerable<ResponseTodayScheduleForDriverModel>> GetAllTodayAsync()
        {
            var driverId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(driverId, out Guid driverIdGuid);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var dayOfWeek = (int)DateTime.Today.DayOfWeek;
            dayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;

            var scheduleRepo = _unitOfWork.GetRepository<Schedule>();
            var overrideRepo = _unitOfWork.GetRepository<ScheduleOverride>();

            var schedules = await scheduleRepo.Entities
                .Where(s => s.DriverId == driverIdGuid && !s.DeletedTime.HasValue)
                .Include(s => s.Shuttle)
                .Include(s => s.Driver)
                .Include(s => s.SchoolShift)
                .Include(s => s.Route)
                    .ThenInclude(r => r.RouteStops)
                        .ThenInclude(rs => rs.Stop)
                .ToListAsync();

            var overrides = await overrideRepo.Entities
                .Where(o => o.Date == today && o.OverrideUserId == driverIdGuid && !o.DeletedTime.HasValue)
                .Include(x => x.Schedule)
                    .ThenInclude(s => s.SchoolShift)
                .Include(x => x.Schedule)
                    .ThenInclude(s => s.Route)
                        .ThenInclude(r => r.RouteStops)
                            .ThenInclude(rs => rs.Stop)
                .Include(x => x.Shuttle)
                .Include(x => x.OverrideUser)
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

                var expectedStudentIds = await _unitOfWork.GetRepository<UserSchoolShift>().Entities
                    .Where(us => us.SchoolShiftId == s.SchoolShiftId)
                    .Select(us => us.StudentId)
                    .ToListAsync();

                var attendedCount = await _unitOfWork.GetRepository<Attendance>().Entities
                    .Where(a =>
                        expectedStudentIds.Contains(a.HistoryTicket.UserId) &&
                        a.Trip.TripDate == today &&
                        a.Trip.Schedule.RouteId == s.RouteId &&
                        a.Trip.Schedule.SchoolShiftId == s.SchoolShiftId &&
                        (a.Status == AttendanceStatusEnum.CHECKED_IN || a.Status == AttendanceStatusEnum.CHECKED_OUT))
                    .Select(a => a.HistoryTicket.UserId)
                    .Distinct()
                    .CountAsync();

                var shuttle = s.Shuttle;

                responseList.Add(new ResponseTodayScheduleForDriverModel
                {
                    Id = s.Id,
                    RouteId = s.RouteId,
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
                    Route = routeInfo
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
                        a.Trip.TripDate == today &&
                        a.Trip.Schedule.RouteId == s.RouteId &&
                        a.Trip.Schedule.SchoolShiftId == s.SchoolShiftId &&
                        (a.Status == AttendanceStatusEnum.CHECKED_IN || a.Status == AttendanceStatusEnum.CHECKED_OUT))
                    .Select(a => a.HistoryTicket.UserId)
                    .Distinct()
                    .CountAsync();

                var shuttle = o.Shuttle;

                responseList.Add(new ResponseTodayScheduleForDriverModel
                {
                    Id = s.Id,
                    RouteId = s.RouteId,
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
                    Route = routeInfo
                });
            }

            return responseList.OrderBy(x => TimeOnly.Parse(x.StartTime));
        }

        public async Task<ResponseScheduleModel> GetByIdAsync(Guid scheduleId)
        {
            var schedule = await _unitOfWork.GetRepository<Schedule>().GetByIdAsync(scheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không tồn tại.");

            if (schedule.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình đã bị xóa.");

            return _mapper.Map<ResponseScheduleModel>(schedule);
        }

        public async Task CreateAsync(CreateScheduleModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(model.RouteId)
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

                var existingSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                    (x.ShuttleId == scheduleDetail.ShuttleId ||
                    x.DriverId == scheduleDetail.DriverId) &&
                    !x.DeletedTime.HasValue &&
                    model.From <= x.To &&
                    model.To >= x.From
                );

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
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Giờ khởi hành {timeStr} phải nhỏ hơn hoặc bằng giờ bắt đầu của ca ({GetSchoolShiftDescription(schoolShift)} lúc {schoolShift.Time}).");
                    }

                    var timeDiffInSeconds = (schoolShift.Time.ToTimeSpan() - time.ToTimeSpan()).TotalSeconds;
                    if (timeDiffInSeconds < totalDuration)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Giờ khởi hành {timeStr} phải cách giờ bắt đầu ca ít nhất {totalDuration / 60} phút để kịp di chuyển.");
                    }
                }

                else if (schoolShift.ShiftType == ShiftTypeEnum.END && time < schoolShift.Time)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                        $"Giờ khởi hành {timeStr} phải lớn hơn hoặc bằng giờ kết thúc ca ({GetSchoolShiftDescription(schoolShift)} lúc {schoolShift.Time}).");
                }

                var direction = schoolShift.ShiftType == ShiftTypeEnum.START
                    ? GeneralEnum.RouteDirectionEnum.IN_BOUND
                    : GeneralEnum.RouteDirectionEnum.OUT_BOUND;
                var days = scheduleDetail.DayOfWeeks.Select(d => d.DayOfWeek.Trim().ToUpper()).ToList();
                var binaryDayOfWeek = ConvertToBinaryDayOfWeek(days);

                foreach (var day in days)
                {
                    var dayIndex = ConvertDayOfWeekToIndex(day);

                    foreach (var existing in existingSchedules)
                    {
                        if (existing.DriverId == scheduleDetail.DriverId &&
                            existing.SchoolShiftId == schoolShift.Id &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1' &&
                            existing.RouteId != model.RouteId)
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế {driver.FullName} đã có ca ở tuyến {existing.Route.RouteName}.");
                        }

                        if (existing.DriverId == scheduleDetail.DriverId &&
                            existing.SchoolShiftId == schoolShift.Id &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1' &&
                            existing.RouteId != model.RouteId)
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe {shuttle.Name} đã có ca ở tuyến {existing.Route.RouteName}.");
                        }

                        if (existing.DriverId == scheduleDetail.DriverId &&
                            existing.SchoolShiftId == schoolShift.Id &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế {existing.Driver.FullName} đã được phân công ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime}.");
                        }
                    }

                    foreach (var newItem in newSchedules)
                    {
                        if (newItem.DriverId == scheduleDetail.DriverId &&
                            newItem.SchoolShiftId == schoolShift.Id &&
                            newItem.Direction == direction &&
                            newItem.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế {newItem.Driver.FullName} đang được tạo mới ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} bị trùng với một ca khác.");
                        }
                    }

                    foreach (var existing in existingSchedules)
                    {
                        if (existing.ShuttleId == scheduleDetail.ShuttleId &&
                            existing.SchoolShiftId == schoolShift.Id &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe {existing.Shuttle.Name} đã được phân công ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime}.");
                        }
                    }

                    foreach (var newItem in newSchedules)
                    {
                        if (newItem.ShuttleId == scheduleDetail.ShuttleId &&
                            newItem.SchoolShiftId == schoolShift.Id &&
                            newItem.Direction == direction &&
                            newItem.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe {newItem.Shuttle.Name} đang được tạo mới ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} bị trùng với một ca khác.");
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
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Giờ khởi hành {timeStr} đã tồn tại trong cùng một thứ với khoảng cách nhỏ hơn 10 phút.");
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
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Trong dữ liệu tạo mới có giờ khởi hành {timeStr} trùng nhau trong cùng một thứ (<10 phút).");
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
                await _unitOfWork.GetRepository<Schedule>().InsertRangeAsync(newSchedules);

            await _stopEstimateService.CreateAsync(newSchedules, model.RouteId);

            var allSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(s => s.RouteId == model.RouteId && !s.DeletedTime.HasValue);

            if (allSchedules != null && allSchedules.Any())
            {
                var minTime = allSchedules.Min(s => s.DepartureTime);
                var maxTime = allSchedules.Max(s => s.DepartureTime);

                route.OperatingTime = $"{minTime:HH\\:mm} - {maxTime:HH\\:mm}";

                var earliestSchedule = allSchedules.OrderBy(s => s.DepartureTime).FirstOrDefault();
                if (earliestSchedule != null)
                {
                    var stopEstimates = await _unitOfWork.GetRepository<StopEstimate>().FindAllAsync(se =>
                        se.ScheduleId == earliestSchedule.Id);

                    if (stopEstimates != null && stopEstimates.Any())
                    {
                        var earliest = stopEstimates.Min(se => se.ExpectedTime);
                        var latest = stopEstimates.Max(se => se.ExpectedTime);
                        route.RunningTime = (latest - earliest).TotalSeconds.ToString();
                    }
                }

                route.LastUpdatedBy = userId;
                route.LastUpdatedTime = DateTime.UtcNow;

                await _unitOfWork.GetRepository<Route>().UpdateAsync(route);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid scheduleId, UpdateScheduleModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.DepartureTime = model.DepartureTime.Trim();

            var schedule = await _unitOfWork.GetRepository<Schedule>().GetByIdAsync(scheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không tồn tại.");

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(schedule.RouteId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            var shuttle = await _unitOfWork.GetRepository<Shuttle>().GetByIdAsync(model.ShuttleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, $"Xe không tồn tại.");

            if (shuttle.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {shuttle.Name} đã bị xóa.");

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
                !x.DeletedTime.HasValue);

            foreach (var day in days)
            {
                var dayIndex = ConvertDayOfWeekToIndex(day);

                foreach (var existing in existingSchedules)
                {
                    if (existing.DriverId == model.DriverId &&
                        existing.SchoolShiftId == schoolShift.Id &&
                        existing.Direction == direction &&
                        existing.DayOfWeek[dayIndex] == '1')
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Tài xế {existing.Driver.FullName} đã có ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime}.");
                    }

                    if (existing.ShuttleId == model.ShuttleId &&
                        existing.SchoolShiftId == schoolShift.Id &&
                        existing.Direction == direction &&
                        existing.DayOfWeek[dayIndex] == '1')
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe {existing.Shuttle.Name} đã có ca {GetSchoolShiftDescription(schoolShift)} vào {ConvertDayOfWeekToVietnamese(day)} lúc {existing.DepartureTime}.");
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
                                $"Có lịch trình khác với giờ khởi hành gần {model.DepartureTime} trong cùng thứ.");
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
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid scheduleId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var schedule = await _unitOfWork.GetRepository<Schedule>().GetByIdAsync(scheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không tồn tại.");

            if (schedule.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình đã bị xóa.");

            schedule.LastUpdatedTime = CoreHelper.SystemTimeNow;
            schedule.LastUpdatedBy = userId;
            schedule.DeletedTime = CoreHelper.SystemTimeNow;
            schedule.DeletedBy = userId;

            await _unitOfWork.GetRepository<Schedule>().UpdateAsync(schedule);
            await _unitOfWork.SaveAsync();
        }

        #region Private Methods

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

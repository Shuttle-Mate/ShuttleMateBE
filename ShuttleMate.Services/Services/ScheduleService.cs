using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.Enum;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.ScheduleModelViews;
using ShuttleMate.ModelViews.StopEstimateModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly VietMapSettings _vietMapSettings;
        private readonly HttpClient _httpClient;

        public ScheduleService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IOptions<VietMapSettings> vietMapSettings, HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _vietMapSettings = vietMapSettings.Value;
            _httpClient = httpClient;
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
            var routeRepo = _unitOfWork.GetRepository<Route>();

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

                var startTime = s.DepartureTime;
                var endTime = startTime.AddHours(1);
                var routeStops = s.Route?.RouteStops?.Where(x => !x.DeletedTime.HasValue).ToList() ?? new();
                var routeInfo = BuildRouteInfo(routeStops, s.Direction);

                responseList.Add(new ResponseTodayScheduleForDriverModel
                {
                    Id = s.Id,
                    RouteCode = s.Route?.RouteCode ?? "",
                    RouteName = s.Route?.RouteName ?? "",
                    StartTime = startTime.ToString("HH:mm"),
                    EndTime = endTime.ToString("HH:mm"),
                    LicensePlate = s.Shuttle?.LicensePlate ?? "",
                    AttendedStudentCount = 13,
                    ExpectedStudentCount = 30,
                    EstimatedDuration = (endTime - startTime).ToString(@"hh\:mm"),
                    Direction = s.Direction.ToString(),
                    Route = routeInfo
                });
            }

            foreach (var o in overrides)
            {
                var s = o.Schedule;
                var startTime = s.DepartureTime;
                var endTime = startTime.AddHours(1);
                var routeStops = s.Route?.RouteStops?.Where(x => !x.DeletedTime.HasValue).ToList() ?? new();

                var routeInfo = BuildRouteInfo(routeStops, s.Direction);

                responseList.Add(new ResponseTodayScheduleForDriverModel
                {
                    Id = s.Id,
                    RouteCode = s.Route?.RouteCode ?? "",
                    RouteName = s.Route?.RouteName ?? "",
                    StartTime = startTime.ToString("HH:mm"),
                    EndTime = endTime.ToString("HH:mm"),
                    LicensePlate = o.Shuttle?.LicensePlate ?? "",
                    AttendedStudentCount = 13,
                    ExpectedStudentCount = 30,
                    EstimatedDuration = (endTime - startTime).ToString(@"hh\:mm"),
                    Direction = s.Direction.ToString(),
                    Route = routeInfo
                });
            }

            return responseList.OrderBy(x => TimeOnly.Parse(x.StartTime));
        }

        public async Task CreateAsync(CreateScheduleModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(model.RouteId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            var newSchedules = new List<Schedule>();

            foreach (var scheduleDetail in model.Schedules)
            {
                var shuttle = await _unitOfWork.GetRepository<Shuttle>().GetByIdAsync(scheduleDetail.ShuttleId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Xe không tồn tại.");

                if (shuttle.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Xe đã bị xóa.");

                if (!shuttle.IsActive)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Xe trong trạng thái không hoạt động.");

                var driver = await _unitOfWork.GetRepository<User>().GetByIdAsync(scheduleDetail.DriverId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tài xế không tồn tại.");

                if (driver.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài xế đã bị xóa.");

                if (driver.Violate == true)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài xế đã bị khóa.");

                var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().GetByIdAsync(scheduleDetail.SchoolShiftId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại.");

                if (schoolShift.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ca học đã bị xóa.");

                var existingSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                    x.RouteId == model.RouteId &&
                    x.ShuttleId == scheduleDetail.ShuttleId &&
                    x.DriverId == scheduleDetail.DriverId &&
                    x.SchoolShiftId == scheduleDetail.SchoolShiftId &&
                    !x.DeletedTime.HasValue
                );

                var timeStr = scheduleDetail.DepartureTime;

                if (!TimeOnly.TryParse(timeStr, out var time))
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành không hợp lệ: {timeStr}");

                var direction = schoolShift.ShiftType == ShiftTypeEnum.START
                    ? GeneralEnum.RouteDirectionEnum.IN_BOUND
                    : GeneralEnum.RouteDirectionEnum.OUT_BOUND;

                if (schoolShift.ShiftType == ShiftTypeEnum.START && time > schoolShift.Time)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                        $"Giờ khởi hành {timeStr} phải nhỏ hơn hoặc bằng giờ bắt đầu của ca ({schoolShift.Time}).");
                }

                else if (schoolShift.ShiftType == ShiftTypeEnum.END && time < schoolShift.Time)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                        $"Giờ khởi hành {timeStr} phải lớn hơn hoặc bằng giờ kết thúc ca ({schoolShift.Time}).");
                }

                var days = scheduleDetail.DayOfWeeks.Select(d => d.DayOfWeek.Trim().ToUpper()).ToList();
                var binaryDayOfWeek = ConvertToBinaryDayOfWeek(days);

                foreach (var day in days)
                {
                    var dayIndex = ConvertDayOfWeekToIndex(day);

                    foreach (var existing in existingSchedules)
                    {
                        if (existing.SchoolShiftId == schoolShift.Id &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế đã được phân công ca {schoolShift.SessionType} vào ngày {day}.");
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
                                $"Tài xế đang được tạo mới ca {schoolShift.SessionType} vào ngày {day} trùng với một ca khác.");
                        }
                    }

                    foreach (var existing in existingSchedules)
                    {
                        if (existing.SchoolShiftId == schoolShift.Id &&
                            existing.Direction == direction &&
                            existing.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe đã được phân công ca {schoolShift.SessionType} vào ngày {day}.");
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
                                $"Xe đang được tạo mới ca {schoolShift.SessionType} vào ngày {day} trùng với một ca khác.");
                        }
                    }
                }

                foreach (var existing in existingSchedules)
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

            var routeStops = await _unitOfWork.GetRepository<RouteStop>().Entities
                .Where(rs => rs.RouteId == model.RouteId)
                .Include(rs => rs.Stop)
                .ToListAsync();

            if (!routeStops.Any())
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không có điểm dừng nào cho tuyến xe này.");

            var waypoints = routeStops
                .OrderBy(rs => rs.StopOrder)
                .Select(rs => $"{rs.Stop.Lat},{rs.Stop.Lng}")
                .ToList();

            var vietMapRouteApiUrl = "https://maps.vietmap.vn/api/route?api-version=1.1";
            var routeRequestUrl = $"{vietMapRouteApiUrl}&apikey={_vietMapSettings.ApiKey}&point={string.Join("&point=", waypoints)}&vehicle=motorcycle";

            var response = await _httpClient.GetAsync(routeRequestUrl);
            if (!response.IsSuccessStatusCode)
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Lỗi khi gọi API VietMap.");

            var responseContent = await response.Content.ReadAsStringAsync();
            var routeApiResponse = JsonConvert.DeserializeObject<ResponseVietMapRouteApi>(responseContent);

            if (routeApiResponse?.Code != "OK")
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Dữ liệu trả về từ VietMap không hợp lệ.");

            var path = routeApiResponse.Paths.FirstOrDefault()
                ?? throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Không có dữ liệu route từ VietMap.");

            int totalTravelTime = path.Time;

            foreach (var schedule in newSchedules)
            {
                for (int i = 0; i < routeStops.Count; i++)
                {
                    var estimatedTime = DateTime.Today
                        .Add(schedule.DepartureTime.ToTimeSpan())
                        .AddMilliseconds(totalTravelTime);

                    var stopEstimate = new StopEstimate
                    {
                        ScheduleId = schedule.Id,
                        StopId = routeStops[i].StopId,
                        ExpectedTime = TimeOnly.FromDateTime(estimatedTime)
                    };

                    await _unitOfWork.GetRepository<StopEstimate>().InsertAsync(stopEstimate);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateScheduleModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            if (model.RouteId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng cung cấp Id tuyến hợp lệ.");
            }

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(model.RouteId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");
            }

            var departureRepo = _unitOfWork.GetRepository<Schedule>();

            var existingDepartures = await departureRepo.FindAllAsync(d =>
                d.RouteId == model.RouteId && !d.DeletedTime.HasValue);

            await _unitOfWork.GetRepository<Schedule>().DeleteRangeAsync(existingDepartures);

            var timeGroups = model.DepartureTimes
                .GroupBy(x => x.Time.Trim())
                .ToDictionary(g => g.Key, g => g.Select(x => x.DayOfWeek.Trim().ToUpper()).ToList());

            var newDepartureTimes = new List<Schedule>();

            foreach (var timeGroup in timeGroups)
            {
                var timeStr = timeGroup.Key;

                if (!TimeOnly.TryParse(timeStr, out var time))
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành không hợp lệ: {timeStr}");
                }

                var days = timeGroup.Value;
                string binaryDayOfWeek = ConvertToBinaryDayOfWeek(days);

                foreach (var existing in newDepartureTimes)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (binaryDayOfWeek[i] == '1' && existing.DayOfWeek[i] == '1')
                        {
                            var timeDiff = Math.Abs((time.ToTimeSpan() - existing.DepartureTime.ToTimeSpan()).TotalMinutes);
                            if (timeDiff < 15)
                            {
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Giờ khởi hành phải cách nhau ít nhất 15 phút trong cùng một thứ trong tuần.");
                            }
                        }
                    }
                }

                newDepartureTimes.Add(new Schedule
                {
                    RouteId = model.RouteId,
                    DepartureTime = time,
                    DayOfWeek = binaryDayOfWeek,
                    CreatedBy = userId,
                    LastUpdatedBy = userId
                });
            }

            if (newDepartureTimes.Count > 1)
            {
                await departureRepo.InsertRangeAsync(newDepartureTimes);
            }
            else if (newDepartureTimes.Count == 1)
            {
                await departureRepo.InsertAsync(newDepartureTimes[0]);
            }

            await _unitOfWork.SaveAsync();
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
    }
}

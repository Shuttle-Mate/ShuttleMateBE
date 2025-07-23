using AutoMapper;
using Microsoft.AspNetCore.Http;
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

        public ScheduleService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateAsync(CreateScheduleModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(model.RouteId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            if (route.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đã bị xóa.");

            var allSchoolShifts = await _unitOfWork.GetRepository<SchoolShift>().GetAllAsync();

            var newSchedules = new List<Schedule>();

            foreach (var scheduleDetail in model.Schedules)
            {
                var shuttle = await _unitOfWork.GetRepository<Shuttle>().GetByIdAsync(scheduleDetail.ShuttleId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Xe không tồn tại.");

                if (shuttle.DeletedTime.HasValue || !shuttle.IsActive)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Xe không hợp lệ.");

                var driver = await _unitOfWork.GetRepository<User>().GetByIdAsync(scheduleDetail.DriverId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tài xế không tồn tại.");

                if (driver.DeletedTime.HasValue || driver.Violate == true)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài xế không hợp lệ.");

                var existingSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                    x.RouteId == model.RouteId &&
                    x.ShuttleId == scheduleDetail.ShuttleId &&
                    x.DriverId == scheduleDetail.DriverId &&
                    !x.DeletedTime.HasValue
                );

                var timeGroups = scheduleDetail.DepartureTimes
                    .GroupBy(x => x.Time.Trim())
                    .ToDictionary(g => g.Key, g => g.Select(x => x.DayOfWeek.Trim().ToUpper()).ToList());

                foreach (var timeGroup in timeGroups)
                {
                    var timeStr = timeGroup.Key;

                    if (!TimeOnly.TryParse(timeStr, out var time))
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành không hợp lệ: {timeStr}");

                    var matchedShift = allSchoolShifts
                        .Where(x => x.SessionType == 0 && x.ShiftType == 0)
                        .OrderByDescending(x => x.Time)
                        .FirstOrDefault(x => time < x.Time);

                    if (matchedShift == null)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Không tìm thấy ca học phù hợp với giờ khởi hành {timeStr}");

                    var direction = matchedShift.ShiftType == 0
                        ? GeneralEnum.RouteDirectionEnum.IN_BOUND
                        : GeneralEnum.RouteDirectionEnum.OUT_BOUND;

                    var days = timeGroup.Value;
                    string binaryDayOfWeek = ConvertToBinaryDayOfWeek(days);

                    foreach (var day in days)
                    {
                        var dayIndex = ConvertDayOfWeekToIndex(day);

                        foreach (var existing in existingSchedules)
                        {
                            if (existing.SchoolShiftId == matchedShift.Id &&
                                existing.Direction == direction &&
                                existing.DayOfWeek[dayIndex] == '1')
                            {
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Tài xế đã được phân công ca {matchedShift.SessionType.ToString()} vào ngày {day}.");
                            }
                        }

                        foreach (var newItem in newSchedules)
                        {
                            if (newItem.DriverId == scheduleDetail.DriverId &&
                                newItem.SchoolShiftId == matchedShift.Id &&
                                newItem.Direction == direction &&
                                newItem.DayOfWeek[dayIndex] == '1')
                            {
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Tài xế đang được tạo mới ca {matchedShift.SessionType.ToString()} vào ngày {day} trùng với một ca khác.");
                            }
                        }

                        foreach (var existing in existingSchedules)
                        {
                            if (existing.SchoolShiftId == matchedShift.Id &&
                                existing.Direction == direction &&
                                existing.DayOfWeek[dayIndex] == '1')
                            {
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Xe đã được phân công ca {matchedShift.SessionType.ToString()} vào ngày {day}.");
                            }
                        }

                        foreach (var newItem in newSchedules)
                        {
                            if (newItem.ShuttleId == scheduleDetail.ShuttleId &&
                                newItem.SchoolShiftId == matchedShift.Id &&
                                newItem.Direction == direction &&
                                newItem.DayOfWeek[dayIndex] == '1')
                            {
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Xe đang được tạo mới ca {matchedShift.SessionType.ToString()} vào ngày {day} trùng với một ca khác.");
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
                                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Giờ khởi hành {timeStr} đã tồn tại trong cùng một thứ với khoảng cách nhỏ hơn 10 phút.");
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
                        DepartureTime = time,
                        DayOfWeek = binaryDayOfWeek,
                        SchoolShiftId = matchedShift.Id,
                        Direction = direction,
                        CreatedBy = userId,
                        LastUpdatedBy = userId
                    });
                }
            }

            if (newSchedules.Any())
                await _unitOfWork.GetRepository<Schedule>().InsertRangeAsync(newSchedules);

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
    }
}

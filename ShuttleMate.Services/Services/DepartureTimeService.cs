using AutoMapper;
using Microsoft.AspNetCore.Http;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.DepartureTimeModelViews;
using ShuttleMate.Services.Services.Infrastructure;

namespace ShuttleMate.Services.Services
{
    public class DepartureTimeService : IDepartureTimeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public DepartureTimeService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateAsync(CreateDepartureTimeModel model)
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

            var existingDepartures = await _unitOfWork.GetRepository<DepartureTime>()
                .FindAllAsync(d => d.RouteId == model.RouteId && !d.DeletedTime.HasValue);

            if (existingDepartures.Any())
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tuyến này đã có giờ khởi hành.");
            }

            var timeGroups = model.DepartureTimes
                .GroupBy(x => x.Time.Trim())
                .ToDictionary(g => g.Key, g => g.Select(x => x.DayOfWeek.Trim().ToUpper()).ToList());

            var newDepartureTimes = new List<DepartureTime>();

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
                            var timeDiff = Math.Abs((time.ToTimeSpan() - existing.Time.ToTimeSpan()).TotalMinutes);
                            if (timeDiff < 15)
                            {
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Giờ khởi hành phải cách nhau ít nhất 15 phút trong cùng một thứ trong tuần.");
                            }
                        }
                    }
                }

                newDepartureTimes.Add(new DepartureTime
                {
                    RouteId = model.RouteId,
                    Time = time,
                    DayOfWeek = binaryDayOfWeek,
                    CreatedBy = userId,
                    LastUpdatedBy = userId
                });
            }

            if (newDepartureTimes.Count > 1)
            {
                await _unitOfWork.GetRepository<DepartureTime>().InsertRangeAsync(newDepartureTimes);
            }
            else if (newDepartureTimes.Count == 1)
            {
                await _unitOfWork.GetRepository<DepartureTime>().InsertAsync(newDepartureTimes[0]);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateDepartureTimeModel model)
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

            var departureRepo = _unitOfWork.GetRepository<DepartureTime>();

            var existingDepartures = await departureRepo.FindAllAsync(d =>
                d.RouteId == model.RouteId && !d.DeletedTime.HasValue);

            await _unitOfWork.GetRepository<DepartureTime>().DeleteRangeAsync(existingDepartures);

            var timeGroups = model.DepartureTimes
                .GroupBy(x => x.Time.Trim())
                .ToDictionary(g => g.Key, g => g.Select(x => x.DayOfWeek.Trim().ToUpper()).ToList());

            var newDepartureTimes = new List<DepartureTime>();

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
                            var timeDiff = Math.Abs((time.ToTimeSpan() - existing.Time.ToTimeSpan()).TotalMinutes);
                            if (timeDiff < 15)
                            {
                                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                    $"Giờ khởi hành phải cách nhau ít nhất 15 phút trong cùng một thứ trong tuần.");
                            }
                        }
                    }
                }

                newDepartureTimes.Add(new DepartureTime
                {
                    RouteId = model.RouteId,
                    Time = time,
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
    }
}

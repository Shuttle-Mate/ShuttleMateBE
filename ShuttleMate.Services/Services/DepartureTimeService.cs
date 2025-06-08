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

        public async Task<IEnumerable<ResponseDepartureTimeModel>> GetAllAsync()
        {
            var departureTimes = await _unitOfWork.GetRepository<DepartureTime>().FindAllAsync(a => !a.DeletedTime.HasValue);

            if (!departureTimes.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có thời gian khởi hành nào.");
            }

            var groupedAndSortedDepartureTimes = departureTimes
                .GroupBy(d => d.RouteId)
                .Select(group => new
                {
                    RouteId = group.Key,
                    DepartureTimes = group.OrderBy(d => d.Departure).ToList()
                })
                .ToList();

            var result = new List<ResponseDepartureTimeModel>();
            foreach (var group in groupedAndSortedDepartureTimes)
            {
                foreach (var departureTime in group.DepartureTimes)
                {
                    var mappedDepartureTime = _mapper.Map<ResponseDepartureTimeModel>(departureTime);
                    result.Add(mappedDepartureTime);
                }
            }

            return result;
        }

        public async Task<ResponseDepartureTimeModel> GetByIdAsync(Guid id)
        {
            var departureTime = await _unitOfWork.GetRepository<DepartureTime>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Thời gian khởi hành không tồn tại.");

            if (departureTime.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Thời gian khởi hành đã bị xóa.");
            }

            return _mapper.Map<ResponseDepartureTimeModel>(departureTime);
        }

        public async Task CreateAsync(CreateDepartureTimeModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid cb);
            model.TrimAllStrings();

            if (model.RouteId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vui lòng điền mã tuyến hợp lệ.");
            }

            if (!TimeOnly.TryParse(model.Departure, out var departureTime))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền giờ khởi hành hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(model.DayOfWeek))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vui lòng điền ngày trong tuần.");
            }
            
            if (model.DayOfWeek.Length != 7 || !model.DayOfWeek.All(c => c == '0' || c == '1'))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền ngày trong tuần hợp lệ.");
            }

            var existingDepartures = await _unitOfWork.GetRepository<DepartureTime>()
        .FindAllAsync(d => d.RouteId == model.RouteId && !d.DeletedTime.HasValue);

            foreach (var existingDeparture in existingDepartures)
            {
                var timeDifference = Math.Abs((departureTime.ToTimeSpan() - existingDeparture.Departure.ToTimeSpan()).TotalMinutes);

                if (timeDifference < 15)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giờ khởi hành của cùng một tuyến phải cách nhau ít nhất 15 phút.");
                }
            }

            var newDepartureTime = new DepartureTime
            {
                RouteId = model.RouteId,
                Departure = departureTime,
                DayOfWeek = model.DayOfWeek,
                CreatedBy = userId,
                LastUpdatedBy = userId
            };

            await _unitOfWork.GetRepository<DepartureTime>().InsertAsync(newDepartureTime);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid id, UpdateDepartureTimeModel model)
        {
            var departureTime = await _unitOfWork.GetRepository<DepartureTime>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Thời gian khởi hành không tồn tại.");

            if (departureTime.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Thời gian khởi hành đã bị xóa.");
            }

            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid cb);
            model.TrimAllStrings();

            if (model.RouteId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mã tuyến hợp lệ.");
            }

            if (model.DayOfWeek.Length != 7 || !model.DayOfWeek.All(c => c == '0' || c == '1'))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền ngày trong tuần hợp lệ.");
            }

            TimeOnly departureTimeParsed = default;

            if (!TimeOnly.TryParse(model.Departure, out departureTimeParsed))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền giờ khởi hành hợp lệ.");
            }

            var existingDepartures = await _unitOfWork.GetRepository<DepartureTime>()
        .FindAllAsync(d => d.RouteId == model.RouteId && d.Id != id && !d.DeletedTime.HasValue);

            foreach (var existingDeparture in existingDepartures)
            {
                var timeDifference = Math.Abs((departureTimeParsed.ToTimeSpan() - existingDeparture.Departure.ToTimeSpan()).TotalMinutes);

                if (timeDifference < 15)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giờ khởi hành của cùng một tuyến phải cách nhau ít nhất 15 phút.");
                }
            }

            departureTime.RouteId = model.RouteId;
            departureTime.DayOfWeek = model.DayOfWeek;
            departureTime.Departure = departureTimeParsed;
            departureTime.LastUpdatedTime = CoreHelper.SystemTimeNow;
            departureTime.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<DepartureTime>().UpdateAsync(departureTime);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid cb);

            var departureTime = await _unitOfWork.GetRepository<DepartureTime>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Thời gian khởi hành không tồn tại.");

            if (departureTime.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Thời gian khởi hành đã bị xóa.");
            }

            departureTime.LastUpdatedTime = CoreHelper.SystemTimeNow;
            departureTime.LastUpdatedBy = userId;
            departureTime.DeletedTime = CoreHelper.SystemTimeNow;
            departureTime.DeletedBy = userId;

            await _unitOfWork.GetRepository<DepartureTime>().UpdateAsync(departureTime);
            await _unitOfWork.SaveAsync();
        }
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.StopEstimateModelViews;

namespace ShuttleMate.Services.Services
{
    public class StopEstimateService : IStopEstimateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly VietMapSettings _vietMapSettings;
        private readonly HttpClient _httpClient;

        public StopEstimateService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IOptions<VietMapSettings> vietMapSettings, HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _vietMapSettings = vietMapSettings.Value;
            _httpClient = httpClient;
        }

        private const string VietMapRouteApiUrl = "https://maps.vietmap.vn/api/route?api-version=1.1";

        public async Task<IEnumerable<ResponseStopEstimateModel>> GetAllAsync()
        {
            var stopEstimates = await _unitOfWork.GetRepository<StopEstimate>().FindAllAsync(se => !se.DeletedTime.HasValue);

            if (!stopEstimates.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không có thời gian ước tính nào.");
            }

            var sortedStopEstimates = stopEstimates
                .OrderBy(se => se.ExpectedTime)
                .ToList();

            return _mapper.Map<IEnumerable<ResponseStopEstimateModel>>(sortedStopEstimates);
        }

        public async Task<IEnumerable<ResponseStopEstimateModel>> GetByRouteIdAsync(Guid routeId)
        {
            var departureTimes = await _unitOfWork.GetRepository<DepartureTime>()
                .FindAllAsync(d => d.RouteId == routeId && !d.DeletedTime.HasValue);

            if (!departureTimes.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không có thời gian khởi hành nào cho tuyến xe này.");
            }

            var stopEstimates = await _unitOfWork.GetRepository<StopEstimate>()
                .FindAllAsync(se => !se.DeletedTime.HasValue &&
                    departureTimes.Select(dt => dt.Id).Contains(se.DepartureTimeId));

            if (!stopEstimates.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không có thời gian ước tính nào cho tuyến xe này.");
            }
            
            var sortedStopEstimates = stopEstimates
                .OrderBy(se => se.ExpectedTime)
                .ToList();

            return _mapper.Map<IEnumerable<ResponseStopEstimateModel>>(sortedStopEstimates);
        }

        public async Task CreateAsync(Guid routeId)
        {
            var route = await _unitOfWork.GetRepository<Route>().GetByIdAsync(routeId);
            if (route == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến xe không tồn tại.");
            }

            var departureTimes = await _unitOfWork.GetRepository<DepartureTime>().FindAllAsync(d => d.RouteId == routeId && !d.DeletedTime.HasValue);
            if (!departureTimes.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không có thời gian khởi hành nào cho tuyến xe này.");
            }

            var stops = await _unitOfWork.GetRepository<Stop>().FindAllAsync(s => s.RouteStops.Any(rs => rs.RouteId == routeId));
            if (!stops.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không có điểm dừng nào cho tuyến xe này.");
            }

            foreach (var departureTime in departureTimes)
            {
                foreach (var stop in stops)
                {
                    var estimatedTime = await CalculateStopEstimateAsync(route, departureTime, stop);
                    if (estimatedTime.HasValue)
                    {
                        var stopEstimate = new StopEstimate
                        {
                            StopId = stop.Id,
                            DepartureTimeId = departureTime.Id,
                            ExpectedTime = estimatedTime.Value
                        };

                        await _unitOfWork.GetRepository<StopEstimate>().InsertAsync(stopEstimate);
                        await _unitOfWork.SaveAsync();
                    }
                }
            }
        }

        private async Task<TimeOnly?> CalculateStopEstimateAsync(Route route, DepartureTime departureTime, Stop stop)
        {
            var waypoints = route.RouteStops.Select(rs => $"{rs.Stop.Lat},{rs.Stop.Lng}").ToList();

            var routeRequestUrl = BuildRouteRequestUrl(waypoints);

            var response = await _httpClient.GetAsync(routeRequestUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Lỗi khi gọi API VietMap.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var routeApiResponse = JsonConvert.DeserializeObject<ResponseVietMapRouteApi>(responseContent);

            if (routeApiResponse?.Code != "OK")
            {
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Có lỗi xảy ra khi gọi API VietMap.");
            }

            int stopIndex = -1;

            foreach (var routeStopWithIndex in route.RouteStops.Select((value, idx) => new { value, idx }))
            {
                var routeStop = routeStopWithIndex.value;
                var index = routeStopWithIndex.idx;

                if (routeStop.StopId == stop.Id)
                {
                    stopIndex = index;
                    break;
                }
            }

            if (stopIndex == -1) return null;

            var estimatedTimeInSeconds = routeApiResponse?.Paths[0]?.Instructions[stopIndex]?.Time;

            if (estimatedTimeInSeconds.HasValue)
            {
                var departureDateTime = DateTime.Today.Add(departureTime.Time.ToTimeSpan());

                var expectedDateTime = departureDateTime.AddSeconds(estimatedTimeInSeconds.Value);

                var expectedTime = TimeOnly.FromDateTime(expectedDateTime);
                return expectedTime;
            }

            return null;
        }

        private string BuildRouteRequestUrl(List<string> waypoints)
        {
            var points = string.Join("&point=", waypoints);

            return $"{VietMapRouteApiUrl}&apikey={_vietMapSettings.ApiKey}&point={points}&vehicle=bus";
        }
    }
}

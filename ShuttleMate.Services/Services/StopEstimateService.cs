using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.StopEstimateModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

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

        private const string VietMapMatrixApiUrl = "https://maps.vietmap.vn/api/matrix?api-version=1.1";

        public async Task CreateAsync(List<Schedule> schedules, Guid routeId)
        {
            var allRouteStops = await _unitOfWork.GetRepository<RouteStop>().Entities
                .Where(rs => rs.RouteId == routeId)
                .Include(rs => rs.Stop)
                .ToListAsync();

            if (!allRouteStops.Any())
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không có điểm dừng nào cho tuyến xe này.");

            foreach (var schedule in schedules)
            {
                var routeStops = schedule.Direction == RouteDirectionEnum.IN_BOUND
                    ? allRouteStops.OrderBy(rs => rs.StopOrder).ToList()
                    : allRouteStops.OrderByDescending(rs => rs.StopOrder).ToList();

                var waypoints = routeStops.Select(rs => $"{rs.Stop.Lat},{rs.Stop.Lng}").ToList();
                var pointParams = string.Join("&", waypoints.Select(p => $"point={p}"));

                var matrixUrl = $"{VietMapMatrixApiUrl}"
                              + $"&apikey={_vietMapSettings.ApiKey}"
                              + $"&{pointParams}"
                              + $"&vehicle=car"
                              + $"&points_encoded=false"
                              + $"&annotations=duration";

                var response = await _httpClient.GetAsync(matrixUrl);
                if (!response.IsSuccessStatusCode)
                    throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Lỗi khi gọi API VietMap Matrix.");

                var content = await response.Content.ReadAsStringAsync();
                var matrixResult = JsonConvert.DeserializeObject<ResponseVietMapMatrixModel>(content);

                if (matrixResult?.Durations == null || matrixResult.Durations.Count == 0)
                    throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Không có dữ liệu duration từ Matrix API.");

                var durationsMatrix = matrixResult.Durations;

                double accumulatedSeconds = 0;

                for (int i = 0; i < routeStops.Count; i++)
                {
                    if (i > 0)
                    {
                        accumulatedSeconds += durationsMatrix[i - 1][i];

                        accumulatedSeconds += 300;
                    }

                    var estimatedTime = DateTime.Today
                        .Add(schedule.DepartureTime.ToTimeSpan())
                        .AddSeconds(accumulatedSeconds);

                    var stopEstimate = new StopEstimate
                    {
                        ScheduleId = schedule.Id,
                        StopId = routeStops[i].StopId,
                        ExpectedTime = TimeOnly.FromDateTime(RoundUpToNearest5Minutes(estimatedTime))
                    };

                    await _unitOfWork.GetRepository<StopEstimate>().InsertAsync(stopEstimate);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(List<Schedule> schedules, Guid routeId)
        {
            var scheduleIds = schedules.Select(s => s.Id).ToList();

            var existingEstimates = await _unitOfWork.GetRepository<StopEstimate>().Entities
                .Where(se => scheduleIds.Contains(se.ScheduleId))
                .ToListAsync();

            await _unitOfWork.GetRepository<StopEstimate>().DeleteRangeAsync(existingEstimates);

            await CreateAsync(schedules, routeId);

            await _unitOfWork.SaveAsync();
        }

        private DateTime RoundUpToNearest5Minutes(DateTime dt)
        {
            int extraMinutes = 5 - dt.Minute % 5;
            if (extraMinutes == 5 && dt.Second == 0 && dt.Millisecond == 0)
                return new DateTime(dt.Ticks);

            return dt.AddMinutes(extraMinutes).AddSeconds(-dt.Second).AddMilliseconds(-dt.Millisecond);
        }
    }
}

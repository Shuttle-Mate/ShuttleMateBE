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
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopEstimateModelViews;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;

namespace ShuttleMate.Services.Services
{
    public class RouteStopService : IRouteStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly VietMapSettings _vietMapSettings;
        private readonly HttpClient _httpClient;
        private readonly IRouteService _routeService;

        public RouteStopService(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor, IOptions<VietMapSettings> vietMapSettings, HttpClient httpClient, IRouteService routeService)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _vietMapSettings = vietMapSettings.Value;
            _httpClient = httpClient;
            _routeService = routeService;
        }

        public async Task AssignStopsToRouteAsync(AssignStopsToRouteModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            try
            {
                // Kiểm tra Route có tồn tại
                var route = await _unitOfWork.GetRepository<Route>()
                    .Entities.FirstOrDefaultAsync(r => r.Id == model.RouteId && !r.DeletedTime.HasValue);
                if (route == null)
                    throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

                // Kiểm tra stops tồn tại
                var stops = await _unitOfWork.GetRepository<Stop>().Entities
                    .Where(s => model.StopIds.Contains(s.Id) && !s.DeletedTime.HasValue)
                    .ToListAsync();

                if (stops.Count != model.StopIds.Count)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Một số trạm dừng không tồn tại!");

                // Kiểm tra xem stops đã thuộc về route khác chưa
                var routeStopRepo = _unitOfWork.GetRepository<RouteStop>();
                var existingRouteStops = await routeStopRepo.Entities
                    .Where(rs => model.StopIds.Contains(rs.StopId) &&
                                rs.RouteId != model.RouteId &&
                                !rs.DeletedTime.HasValue)
                    .ToListAsync();

                if (existingRouteStops.Any())
                {
                    var stopIdsInOtherRoutes = existingRouteStops.Select(rs => rs.StopId).Distinct();
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest,
                        $"Các trạm dừng {string.Join(", ", stopIdsInOtherRoutes)} đã thuộc về tuyến khác!");
                }

                // Xoá các Stop cũ chỉ của route hiện tại
                var oldStops = await routeStopRepo.Entities
                    .Where(rs => rs.RouteId == model.RouteId && !rs.DeletedTime.HasValue)
                    .ToListAsync();

                foreach (var old in oldStops)
                {
                    await routeStopRepo.DeleteAsync(old.RouteId, old.StopId);
                }

                // Thêm stops mới
                var orderedStops = model.StopIds
                    .Select((id, index) => new { Id = id, Order = index + 1, Stop = stops.First(s => s.Id == id) })
                    .ToList();

                foreach (var orderedStop in orderedStops)
                {
                    var newRouteStop = new RouteStop
                    {
                        Id = Guid.NewGuid(),
                        RouteId = model.RouteId,
                        StopId = orderedStop.Stop.Id,
                        StopOrder = orderedStop.Order,
                        Duration = 0,
                        CreatedBy = userId,
                        LastUpdatedBy = userId,
                        CreatedTime = DateTime.UtcNow,
                        LastUpdatedTime = DateTime.UtcNow
                    };

                    await routeStopRepo.InsertAsync(newRouteStop);
                }

                await _unitOfWork.SaveAsync();
                await _routeService.UpdateRouteInformationAsync(model.RouteId);
                await _unitOfWork.SaveAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<BasePaginatedList<StopWithRouteResponseModel>> SearchStopWithRoutes(
            double lat,
            double lng,
            Guid schoolId,
            int page = 0,
            int pageSize = 10)
        {
            if (lat == 0 || lng == 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Phải truyền tọa độ lat, lng");

            var rawStops = await _unitOfWork.GetRepository<Stop>()
                .GetQueryable()
                .Where(s => !s.DeletedTime.HasValue)
                .Include(s => s.RouteStops)
                    .ThenInclude(rs => rs.Route)
                .ToListAsync();

            foreach (var stop in rawStops)
            {
                stop.RouteStops = stop.RouteStops
                    .Where(rs => !rs.Route.DeletedTime.HasValue && rs.Route.SchoolId == schoolId)
                    .ToList();
            }

            var filteredStops = rawStops
                .Where(s =>
                    s.RouteStops.All(rs =>
                    {
                        var routeStops = rs.Route.RouteStops
                            .Where(x => !x.Stop.DeletedTime.HasValue)
                            .OrderBy(x => x.StopOrder)
                            .ToList();
                        if (routeStops.Count <= 2) return false;

                        var first = routeStops.First();
                        var last = routeStops.Last();
                        return rs.StopId != first.StopId && rs.StopId != last.StopId;
                    }))
                .ToList();

            if (!filteredStops.Any())
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có trạm hợp lệ sau khi lọc đầu/cuối!");

            var stops = filteredStops;

            if (!stops.Any())
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có trạm nào tồn tại!");

            var userPoint = $"{lat},{lng}";
            var stopPoints = stops.Select(s => $"{s.Lat},{s.Lng}").ToList();
            var allPoints = new List<string> { userPoint };
            allPoints.AddRange(stopPoints);
            var pointParams = string.Join("&", allPoints.Select(p => $"point={p}"));
            var sourceParam = "sources=0";
            var destinationParam = "destinations=" + string.Join(";", Enumerable.Range(1, stopPoints.Count));

            var matrixUrl = $"https://maps.vietmap.vn/api/matrix?api-version=1.1"
                          + $"&apikey={_vietMapSettings.ApiKey}"
                          + $"&{pointParams}"
                          + $"&{sourceParam}"
                          + $"&{destinationParam}"
                          + "&vehicle=foot&points_encoded=false&annotations=distance,duration";

            var response = await _httpClient.GetAsync(matrixUrl);
            if (!response.IsSuccessStatusCode)
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Lỗi khi gọi API VietMap Matrix.");

            var content = await response.Content.ReadAsStringAsync();
            var matrixResult = JsonConvert.DeserializeObject<ResponseVietMapMatrixModel>(content);

            if (matrixResult?.Distances == null || matrixResult.Durations == null ||
                matrixResult.Distances.Count == 0 || matrixResult.Durations.Count == 0)
            {
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Không có dữ liệu distance/duration từ Matrix API.");
            }

            var distanceList = matrixResult.Distances[0];
            var durationList = matrixResult.Durations[0];

            var stopsWithDistance = stops.Select((s, i) => new
            {
                Stop = s,
                Distance = distanceList[i],
                Duration = durationList[i]
            })
            .Where(x => x.Stop.RouteStops.Any(rs => !rs.Route.DeletedTime.HasValue && rs.Route.SchoolId == schoolId))
            .OrderBy(x => x.Distance);

            var totalCount = stopsWithDistance.Count();

            var pagedStops = stopsWithDistance
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            var result = pagedStops.Select(x => new StopWithRouteResponseModel
            {
                StopId = x.Stop.Id,
                StopName = x.Stop.Name,
                Address = x.Stop.Address,
                Distance = Math.Round(x.Distance, 2),
                Duration = Math.Round(x.Duration, 2),
                Routes = x.Stop.RouteStops
                    .Where(rs => !rs.Route.DeletedTime.HasValue && rs.Route.SchoolId == schoolId)
                    .Select(rs => new RouteResponseModel
                    {
                        RouteId = rs.Route.Id,
                        RouteCode = rs.Route.RouteCode,
                        RouteName = rs.Route.RouteName
                    })
                    .Distinct()
                    .ToList()
            }).ToList();

            return new BasePaginatedList<StopWithRouteResponseModel>(result, totalCount, page, pageSize);
        }
    }
}

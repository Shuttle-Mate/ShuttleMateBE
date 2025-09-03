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
        private readonly IEmailService _emailService;

        public RouteStopService(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor, IOptions<VietMapSettings> vietMapSettings, HttpClient httpClient, IRouteService routeService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _vietMapSettings = vietMapSettings.Value;
            _httpClient = httpClient;
            _routeService = routeService;
            _emailService = emailService;
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

                var routeStopRepo = _unitOfWork.GetRepository<RouteStop>();

                // Lấy danh sách stops hiện tại của route
                var currentRouteStops = await routeStopRepo.Entities
                    .Where(rs => rs.RouteId == model.RouteId && !rs.DeletedTime.HasValue)
                    .ToListAsync();

                // Lấy danh sách stops muốn thêm (kiểm tra tồn tại)
                var stopsToAdd = await _unitOfWork.GetRepository<Stop>().Entities
                    .Where(s => model.StopIds.Contains(s.Id) && !s.DeletedTime.HasValue)
                    .ToListAsync();

                if (stopsToAdd.Count != model.StopIds.Count)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Một số trạm dừng không tồn tại!");

                // Xác định stops cần xóa (có trong current nhưng không có trong model.StopIds)
                var stopsToRemove = currentRouteStops
                    .Where(current => !model.StopIds.Contains(current.StopId))
                    .ToList();
                foreach (var stopToRemove in stopsToRemove)
                {
                    // Xóa stops không còn thuộc route - SỬA Ở ĐÂY
                    if (stopsToRemove.Any())
                    {
                        var del = await _unitOfWork.GetRepository<RouteStop>()
                        .Entities.FirstOrDefaultAsync(x => x.StopId == stopToRemove.StopId && x.RouteId == stopToRemove.RouteId && !x.DeletedTime.HasValue);
                        if (del == null)
                            throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến dừng!");
                        await routeStopRepo.DeleteAsync(del);
                    }
                }

                // Xác định stops cần thêm mới (có trong model.StopIds nhưng chưa có trong current)
                var existingStopIds = currentRouteStops.Select(rs => rs.StopId).ToList();
                var stopsToInsert = model.StopIds
                    .Where(stopId => !existingStopIds.Contains(stopId))
                    .ToList();

                // Thêm stops mới vào route
                var newRouteStops = new List<RouteStop>();
                foreach (var stopId in stopsToInsert)
                {
                    var order = model.StopIds.IndexOf(stopId) + 1;

                    var newRouteStop = new RouteStop
                    {
                        Id = Guid.NewGuid(),
                        RouteId = model.RouteId,
                        StopId = stopId,
                        StopOrder = order,
                        Duration = 0,
                        CreatedBy = userId,
                        LastUpdatedBy = userId,
                        CreatedTime = DateTime.UtcNow,
                        LastUpdatedTime = DateTime.UtcNow
                    };

                    newRouteStops.Add(newRouteStop);
                }

                if (newRouteStops.Any())
                {
                    await routeStopRepo.InsertRangeAsync(newRouteStops);
                }

                // Cập nhật thứ tự cho các stops đã tồn tại
                var stopsToUpdate = currentRouteStops
                    .Where(rs => !stopsToRemove.Contains(rs) && model.StopIds.Contains(rs.StopId))
                    .ToList();

                foreach (var existingStop in stopsToUpdate)
                {
                    var newOrder = model.StopIds.IndexOf(existingStop.StopId) + 1;
                    if (existingStop.StopOrder != newOrder)
                    {
                        existingStop.StopOrder = newOrder;
                        existingStop.LastUpdatedBy = userId;
                        existingStop.LastUpdatedTime = DateTime.UtcNow;
                        routeStopRepo.Update(existingStop);
                    }
                }

                // Chỉ save một lần cuối cùng - SỬA Ở ĐÂY
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

            //var rawStops = await _unitOfWork.GetRepository<Stop>()
            //    .GetQueryable()
            //    .Where(s => !s.DeletedTime.HasValue)
            //    .Include(s => s.RouteStops)
            //        .ThenInclude(rs => rs.Route)
            //    .ToListAsync();
            // Query stops + filter luôn RouteStops trong Include để tránh gán lại collection
            var rawStops = await _unitOfWork.GetRepository<Stop>()
                .GetQueryable()
                .Where(s => !s.DeletedTime.HasValue)
                .Include(s => s.RouteStops
                    .Where(rs => !rs.Route.DeletedTime.HasValue && rs.Route.SchoolId == schoolId))
                    .ThenInclude(rs => rs.Route)
                .ToListAsync();

            //foreach (var stop in rawStops)
            //{
            //    stop.RouteStops = stop.RouteStops
            //        .Where(rs => !rs.Route.DeletedTime.HasValue && rs.Route.SchoolId == schoolId)
            //        .ToList();
            //}

            //var filteredStops = rawStops
            //    .Where(s =>
            //        s.RouteStops.All(rs =>
            //        {
            //            var routeStops = rs.Route.RouteStops
            //                .Where(x => !x.Stop.DeletedTime.HasValue)
            //                .OrderBy(x => x.StopOrder)
            //                .ToList();
            //            if (routeStops.Count <= 2) return false;

            //            var first = routeStops.First();
            //            var last = routeStops.Last();
            //            return rs.StopId != first.StopId && rs.StopId != last.StopId;
            //        }))
            //    .ToList();

            // Chỉ giữ lại Stop có RouteStops hợp lệ (bỏ đầu/cuối tuyến)
            var filteredStops = rawStops
                .Where(s => s.RouteStops.Any() &&
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

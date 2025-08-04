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
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopEstimateModelViews;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class RouteStopService : IRouteStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly VietMapSettings _vietMapSettings;
        private readonly HttpClient _httpClient;

        public RouteStopService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IOptions<VietMapSettings> vietMapSettings, HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _vietMapSettings = vietMapSettings.Value;
            _httpClient = httpClient;
        }

        private const string VietMapMatrixApiUrl = "https://maps.vietmap.vn/api/matrix?api-version=1.1";

        public async Task AssignStopsToRouteAsync(AssignStopsToRouteModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            //_unitOfWork.BeginTransaction();

            try
            {
                // Kiểm tra Route có tồn tại
                var route = await _unitOfWork.GetRepository<Route>()
                    .Entities.FirstOrDefaultAsync(r => r.Id == model.RouteId && !r.DeletedTime.HasValue);
                if (route == null)
                    throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

                // Xoá các Stop cũ đã gắn (nếu có)
                var routeStopRepo = _unitOfWork.GetRepository<RouteStop>();
                var oldStops = await routeStopRepo.Entities
                    .Where(rs => rs.RouteId == model.RouteId && !rs.DeletedTime.HasValue)
                    .ToListAsync();

                foreach (var old in oldStops)
                {
                    //old.DeletedTime = DateTime.UtcNow;
                    //old.DeletedBy = userId;
                    //_unitOfWork.DbContext.Entry(old).State = EntityState.Detached; // Ngắt tracking
                    await routeStopRepo.DeleteAsync(old.RouteId, old.StopId);
                    await _unitOfWork.SaveAsync();
                    //_unitOfWork.Detach(old);
                }

                //await routeStopRepo.UpdateRangeAsync(oldStops);
                await _unitOfWork.SaveAsync();

                foreach (var old in oldStops)
                {
                    _unitOfWork.Detach(old);
                }

                var stops = await _unitOfWork.GetRepository<Stop>().Entities
                    .Where(s => model.StopIds.Contains(s.Id) && !s.DeletedTime.HasValue)
                    .ToListAsync();

                if (stops.Count != model.StopIds.Count)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Một số StopId không tồn tại!");

                var orderedStops = model.StopIds
                    .Select((id, index) => new { Id = id, Order = index + 1, Stop = stops.First(s => s.Id == id) })
                    .ToList();

                var waypoints = orderedStops.Select(s => $"{s.Stop.Lat},{s.Stop.Lng}").ToList();
                var pointParams = string.Join("&", waypoints.Select(p => $"point={p}"));

                var matrixUrl = $"{VietMapMatrixApiUrl}"
                              + $"&apikey={_vietMapSettings.ApiKey}"
                              + $"&{pointParams}"
                              + $"&vehicle=car"
                              + $"&points_encoded=false"
                              + $"&annotations=duration,distance";

                var response = await _httpClient.GetAsync(matrixUrl);
                if (!response.IsSuccessStatusCode)
                    throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Lỗi khi gọi API VietMap Matrix.");

                var content = await response.Content.ReadAsStringAsync();
                var matrixResult = JsonConvert.DeserializeObject<ResponseVietMapMatrixModel>(content);

                if (matrixResult?.Durations == null || matrixResult.Durations.Count == 0)
                    throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Không có dữ liệu duration từ Matrix API.");

                var durationsMatrix = matrixResult.Durations;

                for (int i = 0; i < orderedStops.Count; i++)
                {
                    int duration = 0;
                    if (i > 0)
                    {
                        duration = (int)Math.Round(durationsMatrix[i - 1][i]);
                    }

                    var newRouteStop = new RouteStop
                    {
                        Id = Guid.NewGuid(),
                        RouteId = model.RouteId,
                        StopId = orderedStops[i].Stop.Id,
                        StopOrder = orderedStops[i].Order,
                        Duration = duration,
                        CreatedBy = userId,
                        LastUpdatedBy = userId,
                        CreatedTime = DateTime.UtcNow,
                        LastUpdatedTime = DateTime.UtcNow
                    };

                    await routeStopRepo.InsertAsync(newRouteStop);
                }

                await _unitOfWork.SaveAsync();

                if (matrixResult?.Distances == null || matrixResult.Distances.Count == 0)
                    throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Không có dữ liệu distance từ Matrix API.");

                var distancesMatrix = matrixResult.Distances;

                double totalDistance = 0;
                for (int i = 1; i < orderedStops.Count; i++)
                {
                    totalDistance += distancesMatrix[i - 1][i];
                }

                var validStops = await _unitOfWork.GetRepository<RouteStop>().Entities
                    .Where(rs => rs.RouteId == model.RouteId && !rs.DeletedTime.HasValue)
                    .OrderBy(rs => rs.StopOrder)
                    .ToListAsync();

                if (validStops != null && validStops.Count > 1)
                {
                    var travelDurations = validStops.Skip(1).Sum(rs => rs.Duration);
                    var stopTimeBuffer = 300 * (validStops.Count - 1);
                    route.RunningTime = (travelDurations + stopTimeBuffer).ToString();
                }
                else
                {
                    route.RunningTime = "0";
                }

                var stopNames = orderedStops.Select(x => x.Stop.Name).ToList();
                route.InBound = string.Join(" - ", stopNames);
                route.OutBound = string.Join(" - ", stopNames.AsEnumerable().Reverse());
                route.TotalDistance = (decimal?)Math.Round(totalDistance, 2);
                route.LastUpdatedTime = DateTime.UtcNow;
                route.LastUpdatedBy = userId;

                _unitOfWork.GetRepository<Route>().Update(route);

                await _unitOfWork.SaveAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<BasePaginatedList<StopWithRouteResponseModel>> SearchStopWithRoutes(double lat, double lng, GetRouteStopQuery req)
        {
            if (lat == 0 || lng == 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Phải truyền tọa độ lat, lng");

            var stops = await _unitOfWork.GetRepository<Stop>().Entities
                .Where(s => !s.DeletedTime.HasValue)
                .Include(s => s.RouteStops)
                    .ThenInclude(rs => rs.Route)
                .ToListAsync();

            if (!stops.Any())
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có trạm nào tồn tại!");

            var waypoints = stops.Select(s => $"{s.Lat},{s.Lng}").ToList();
            var pointParams = string.Join("&", waypoints.Select(p => $"point={p}"));

            var matrixUrl = $"https://maps.vietmap.vn/api/matrix?api-version=1.1"
                          + $"&apikey={_vietMapSettings.ApiKey}"
                          + $"&point={lat},{lng}&{pointParams}"
                          + "&vehicle=foot&points_encoded=false&annotations=distance,duration";

            var response = await _httpClient.GetAsync(matrixUrl);
            if (!response.IsSuccessStatusCode)
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Lỗi khi gọi API VietMap Matrix.");

            var content = await response.Content.ReadAsStringAsync();
            var matrixResult = JsonConvert.DeserializeObject<ResponseVietMapMatrixModel>(content);

            if (matrixResult?.Distances == null || matrixResult.Durations == null || matrixResult.Distances.Count == 0 || matrixResult.Durations.Count == 0)
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Không có dữ liệu distance/duration từ Matrix API.");

            var distanceList = matrixResult.Distances[0];
            var durationList = matrixResult.Durations[0];

            var stopsWithDistance = stops.Select((s, i) => new
            {
                Stop = s,
                Distance = distanceList[i],
                Duration = durationList[i]
            });

            if (!string.IsNullOrWhiteSpace(req.search))
            {
                var keyword = req.search.ToLower();
                stopsWithDistance = stopsWithDistance.Where(x =>
                    x.Stop.Name.ToLower().Contains(keyword) ||
                    x.Stop.Address.ToLower().Contains(keyword));
            }

            var filtered = stopsWithDistance
                .OrderBy(x => x.Distance)
                .Select(x => new StopWithRouteResponseModel
                {
                    StopId = x.Stop.Id,
                    StopName = x.Stop.Name,
                    Address = x.Stop.Address,
                    Distance = Math.Round(x.Distance, 2),
                    Duration = Math.Round(x.Duration, 2),
                    Routes = x.Stop.RouteStops
                        .Where(rs => !rs.Route.DeletedTime.HasValue && (!req.schoolId.HasValue || rs.Route.SchoolId == req.schoolId))
                        .Select(rs => new RouteResponseModel
                        {
                            RouteId = rs.Route.Id,
                            RouteCode = rs.Route.RouteCode,
                            RouteName = rs.Route.RouteName
                        })
                        .Distinct()
                        .ToList()
                })
                .Where(x => x.Routes.Any());

            var totalCount = filtered.Count();
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var paginated = filtered
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            return new BasePaginatedList<StopWithRouteResponseModel>(paginated, totalCount, page, pageSize);
        }
    }
}

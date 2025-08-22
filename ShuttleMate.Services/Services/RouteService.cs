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
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopEstimateModelViews;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;

namespace ShuttleMate.Services.Services
{
    public class RouteService : IRouteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly VietMapSettings _vietMapSettings;
        private readonly HttpClient _httpClient;

        public RouteService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IOptions<VietMapSettings> vietMapSettings, HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _vietMapSettings = vietMapSettings.Value;
            _httpClient = httpClient;
        }

        private const string VietMapMatrixApiUrl = "https://maps.vietmap.vn/api/matrix?api-version=1.1";

        public async Task CreateRoute(RouteModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Route route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => (x.RouteName == model.RouteName || x.RouteCode == model.RouteCode) && !x.DeletedTime.HasValue);

            School school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.SchoolId && x.IsActive == true && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường học!");

            if (route != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trùng tên hoặc tuyến này đã tồn tại!!");
            }
            else if (model.SchoolId == null || model.SchoolId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trường không được để trống!!");
            }

            var newRoute = _mapper.Map<Route>(model);
            newRoute.CreatedBy = userId;
            newRoute.IsActive = true;
            newRoute.LastUpdatedBy = userId;
            await _unitOfWork.GetRepository<Route>().InsertAsync(newRoute);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteRoute(Guid routeId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == routeId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");
            route.DeletedTime = DateTime.Now;
            route.DeletedBy = userId;
            await _unitOfWork.GetRepository<Route>().UpdateAsync(route);
            await _unitOfWork.SaveAsync();
        }

        public async Task<BasePaginatedList<ResponseRouteModel>> GetAll(GetRouteQuery req)
        {
            string searchKeyword = req.search ?? "";
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var query = _unitOfWork.GetRepository<Route>().Entities
                .Where(x => !x.DeletedTime.HasValue);
            //.OrderBy(x => x.RouteCode);

            if (req.schoolId.HasValue && req.schoolId.Value != Guid.Empty)
            {
                query = query.Where(x => x.SchoolId == req.schoolId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                query = query.Where(x =>
                    x.RouteCode.ToLower().Contains(searchKeyword.ToLower()) ||
                    x.RouteName.ToLower().Contains(searchKeyword.ToLower()) ||
                    x.Description.ToLower().Contains(searchKeyword.ToLower()));
            }

            // Validate and apply sorting
            switch (req.sortBy?.Trim().ToUpperInvariant())
            {
                case "CODE":
                    query = query.OrderBy(x => x.RouteCode);
                    break;
                case "NAME":
                    query = query.OrderBy(x => x.RouteName);
                    break;
                case "PRICE":
                    query = query.OrderBy(x => x.Price);
                    break;
                default:
                    query = query.OrderByDescending(x => x.LastUpdatedTime);
                    break;
            }

            var totalCount = query.Count();

            var routes = await query
                .Skip(req.page * req.pageSize)
                .Take(req.pageSize)
                .ToListAsync();

            if (!routes.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có tuyến nào tồn tại!");
            }

            var result = _mapper.Map<List<ResponseRouteModel>>(routes);

            foreach (var routeModel in result)
            {
                var routeEntity = routes.First(r => r.Id == routeModel.Id);
                var validStops = routeEntity.RouteStops?
                    .Where(rs => !rs.DeletedTime.HasValue)
                    .OrderBy(rs => rs.StopOrder)
                    .ToList();

                if (validStops is not null && validStops.Count > 1)
                {
                    var travelDurations = validStops.Skip(1).Sum(rs => rs.Duration);
                    var stopTimeBuffer = 300 * (validStops.Count - 1);
                    routeModel.TotalDuration = travelDurations + stopTimeBuffer;
                }
                else
                {
                    routeModel.TotalDuration = 0;
                }
            }

            return new BasePaginatedList<ResponseRouteModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseRouteModel> GetById(Guid routeId)
        {
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == routeId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            var result = _mapper.Map<ResponseRouteModel>(route);

            var activeStops = route.RouteStops?
                .Where(rs => !rs.DeletedTime.HasValue)
                .ToList();

            if (activeStops != null && activeStops.Count > 1)
            {
                var sumDuration = activeStops.Sum(rs => rs.Duration);
                result.TotalDuration = sumDuration + (300 * (activeStops.Count - 1));
            }
            else
            {
                result.TotalDuration = 0;
            }

            return result;
        }

        public async Task<BasePaginatedList<StopWithOrderModel>> StopListByRoute(GetRouteStopQuery req, Guid routeId)
        {
            string search = req.search ?? "";
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var query = _unitOfWork.GetRepository<RouteStop>().Entities
                .Where(rs => rs.RouteId == routeId && !rs.DeletedTime.HasValue)
                .Include(rs => rs.Stop)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(rs =>
                    rs.Stop.Name.ToLower().Contains(search) ||
                    rs.Stop.Address.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();

            var routeStops = await query
                .OrderBy(rs => rs.StopOrder)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!routeStops.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có điểm dừng nào cho tuyến này!");
            }

            var result = routeStops
                .Select(rs => new StopWithOrderModel
                {
                    Stop = new BasicStopModel
                    {
                        Id = rs.Stop.Id,
                        Name = rs.Stop.Name,
                        Address = rs.Stop.Address,
                        Lat = rs.Stop.Lat,
                        Lng = rs.Stop.Lng
                    },
                    StopOrder = rs.StopOrder,
                    Duration = rs.Duration
                })
                .ToList();

            return new BasePaginatedList<StopWithOrderModel>(result, totalCount, page, pageSize);
        }

        public async Task UpdateRoute(UpdateRouteModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (string.IsNullOrWhiteSpace(model.RouteName) && string.IsNullOrWhiteSpace(model.RouteCode))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Mã tuyến và tên tuyến không được để trống!");
            }
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            if (model.RouteCode == route.RouteCode || model.RouteName == route.RouteName)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Mã tuyến hoặc tên tuyến đã tồn tại!");
            }

            //route = _mapper.Map<Route>(model);
            _mapper.Map(model, route);
            route.LastUpdatedBy = userId;
            route.LastUpdatedTime = DateTime.Now;
            await _unitOfWork.GetRepository<Route>().UpdateAsync(route);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateRouteInformationAsync(Guid routeId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var route = await _unitOfWork.GetRepository<Route>()
                .GetQueryable()
                .Include(r => r.RouteStops)
                .ThenInclude(rs => rs.Stop)
                .FirstOrDefaultAsync(r => r.Id == routeId && !r.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến không tồn tại.");

            var validRouteStops = route.RouteStops
                .Where(rs => !rs.DeletedTime.HasValue)
                .OrderBy(rs => rs.StopOrder)
                .ToList();

            if (validRouteStops.Count == 0)
            {
                route.InBound = string.Empty;
                route.OutBound = string.Empty;
                route.TotalDistance = 0;
                route.RunningTime = "0";
                route.LastUpdatedTime = CoreHelper.SystemTimeNow;
                route.LastUpdatedBy = userId;

                await _unitOfWork.GetRepository<Route>().UpdateAsync(route);
                return;
            }

            var waypoints = validRouteStops
                .Select(rs => $"{rs.Stop.Lat},{rs.Stop.Lng}")
                .ToList();

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

            if (matrixResult?.Durations == null || matrixResult.Durations.Count == 0 ||
                matrixResult?.Distances == null || matrixResult.Distances.Count == 0)
                throw new ErrorException(StatusCodes.Status500InternalServerError, ResponseCodeConstants.INTERNAL_SERVER_ERROR, "Không có dữ liệu từ Matrix API.");

            var durationsMatrix = matrixResult.Durations;
            var distancesMatrix = matrixResult.Distances;

            for (int i = 0; i < validRouteStops.Count; i++)
            {
                int duration = 0;
                if (i > 0)
                {
                    duration = (int)Math.Round(durationsMatrix[i - 1][i]);
                }

                validRouteStops[i].Duration = duration;
                validRouteStops[i].LastUpdatedTime = DateTime.UtcNow;
                validRouteStops[i].LastUpdatedBy = userId;
            }

            double totalDistance = 0;
            for (int i = 1; i < validRouteStops.Count; i++)
            {
                totalDistance += distancesMatrix[i - 1][i];
            }

            if (validRouteStops.Count > 1)
            {
                var travelDurations = validRouteStops.Skip(1).Sum(rs => rs.Duration);
                var stopTimeBuffer = 300 * (validRouteStops.Count - 1);
                route.RunningTime = (travelDurations + stopTimeBuffer).ToString();
            }
            else
            {
                route.RunningTime = "0";
            }

            var stopNames = validRouteStops.Select(rs => rs.Stop.Name).ToList();
            route.InBound = string.Join(" - ", stopNames);
            route.OutBound = string.Join(" - ", stopNames.AsEnumerable().Reverse());
            route.TotalDistance = (decimal?)Math.Round(totalDistance, 2);
            route.LastUpdatedTime = DateTime.UtcNow;
            route.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<RouteStop>().UpdateRangeAsync(validRouteStops);
            await _unitOfWork.GetRepository<Route>().UpdateAsync(route);
        }
    }
}

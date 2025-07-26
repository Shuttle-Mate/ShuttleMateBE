using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.RouteStopModelViews;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class RouteService : IRouteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;


        public RouteService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateRoute(RouteModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Route route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.RouteName == model.RouteName || x.RouteCode == model.RouteCode);

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
            
            return new BasePaginatedList<ResponseRouteModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseRouteModel> GetById(Guid routeId)
        {
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == routeId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            return _mapper.Map<ResponseRouteModel>(route);
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
                    StopOrder = rs.StopOrder
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

            //route = _mapper.Map<Route>(model);
            _mapper.Map(model, route);
            route.LastUpdatedBy = userId;
            route.LastUpdatedTime = DateTime.Now;
            await _unitOfWork.GetRepository<Route>().UpdateAsync(route);
            await _unitOfWork.SaveAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.Services.Services.Infrastructure;
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

        public async Task<List<ResponseRouteModel>> GetAll()
        {
            var routes = await _unitOfWork.GetRepository<Route>().Entities.Where(x => !x.DeletedTime.HasValue).OrderBy(x => x.RouteCode).ToListAsync();
            if (!routes.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có tuyến nào tồn tại!");
            }
            return _mapper.Map<List<ResponseRouteModel>>(routes);
        }

        public async Task<ResponseRouteModel> GetById(Guid routeId)
        {
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == routeId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            return _mapper.Map<ResponseRouteModel>(route);
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

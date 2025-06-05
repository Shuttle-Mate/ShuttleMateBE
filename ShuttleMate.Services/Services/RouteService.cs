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

namespace ShuttleMate.Services.Services
{
    public class RouteService : IRouteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RouteService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task CreateRoute(RouteModel model)
        {
            Route route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.RouteName == model.RouteName);
            if (route != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trùng tên hoặc tuyến này đã tồn tại!!");
            }
            var newRoute = _mapper.Map<Route>(model);   
            await _unitOfWork.GetRepository<Route>().InsertAsync(newRoute);
            await _unitOfWork.SaveAsync();
        }

        public Task DeleteRoute(Guid routeId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ResponseRouteModel>> GetAll()
        {
            var routes = await _unitOfWork.GetRepository<Route>().Entities.Where(x => !x.DeletedTime.HasValue).OrderBy(x => x.RouteName).ToListAsync();
            if (!routes.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có tuyến nào tồn tại!");
            }
            return _mapper.Map<List<ResponseRouteModel>>(routes);
        }

        public Task<ResponseRouteModel> GetById(Guid routeId)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateRoute(UpdateRouteModel model)
        {
            if (string.IsNullOrWhiteSpace(model.RouteName) && string.IsNullOrWhiteSpace(model.RouteCode))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Mã tuyến và tên tuyến không được để trống!");
            }
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            //route = _mapper.Map<Route>(model);
            _mapper.Map(model,route);
            await _unitOfWork.GetRepository<Route>().UpdateAsync(route);
            await _unitOfWork.SaveAsync();
        }
    }
}

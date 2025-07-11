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
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;

namespace ShuttleMate.Services.Services
{
    public class RouteStopService : IRouteStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public RouteStopService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

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

                await routeStopRepo.UpdateRangeAsync(oldStops);
                await _unitOfWork.SaveAsync();

                foreach (var old in oldStops)
                {
                    _unitOfWork.Detach(old);
                }

                // Gắn Stop mới với StopOrder
                int order = 1;
                foreach (var stopId in model.StopIds)
                {
                    var stopExists = await _unitOfWork.GetRepository<Stop>()
                        .Entities.AnyAsync(s => s.Id == stopId && !s.DeletedTime.HasValue);
                    if (!stopExists)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, $"StopId {stopId} không hợp lệ!");

                    var newRouteStop = new RouteStop
                    {
                        Id = Guid.NewGuid(),
                        RouteId = model.RouteId,
                        StopId = stopId,
                        StopOrder = order++,
                        CreatedBy = userId,
                        LastUpdatedBy = userId,
                        CreatedTime = DateTime.UtcNow,
                        LastUpdatedTime = DateTime.UtcNow
                    };

                    await routeStopRepo.InsertAsync(newRouteStop);
                }

                await _unitOfWork.SaveAsync();
                //_unitOfWork.CommitTransaction();
            }
            catch
            {
                //_unitOfWork.RollBack();
                throw;
            }
        }
    }
}

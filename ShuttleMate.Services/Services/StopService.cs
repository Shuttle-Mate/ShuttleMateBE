using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;

namespace ShuttleMate.Services.Services
{
    public class StopService : IStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IRouteService _routeService;

        public StopService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IRouteService routeService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _routeService = routeService;
        }

        public async Task<BasePaginatedList<ResponseStopModel>> GetAllAsync(
            string? search,
            Guid? wardId,
            bool sortAsc = false,
            int page = 0,
            int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<Stop>()
                .GetQueryable()
                .Include(x => x.Ward)
                .Where(x => !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(lowered) ||
                    x.Address.ToLower().Contains(lowered));
            }

            if (wardId.HasValue && wardId.Value != Guid.Empty)
            {
                query = query.Where(x => x.WardId == wardId.Value);
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ResponseStopModel>>(pagedItems);

            return new BasePaginatedList<ResponseStopModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseStopModel> GetByIdAsync(Guid stopId)
        {
            var stop = await _unitOfWork.GetRepository<Stop>().GetByIdAsync(stopId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng không tồn tại.");

            if (stop.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng đã bị xóa.");

            return _mapper.Map<ResponseStopModel>(stop);
        }

        public async Task CreateAsync(CreateStopModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var existingStop = await _unitOfWork.GetRepository<Stop>().FindAsync(x => x.RefId == model.RefId && !x.DeletedTime.HasValue);

            if (existingStop != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trạm dừng này đã tồn tại.");
            }

            var existingWard = await _unitOfWork.GetRepository<Ward>().FindAsync(w => w.Name.ToLower() == model.WardName.ToLower());

            if (existingWard == null)
            {
                existingWard = new Ward
                {
                    Name = model.WardName
                };

                await _unitOfWork.GetRepository<Ward>().InsertAsync(existingWard);
                await _unitOfWork.SaveAsync();
            }

            var newStop = new Stop
            {
                Name = model.Name.Trim(),
                RefId = model.RefId,
                Address = model.Address,
                Lat = model.Lat,
                Lng = model.Lng,
                WardId = existingWard.Id,
                CreatedBy = userId,
                LastUpdatedBy = userId
            };

            await _unitOfWork.GetRepository<Stop>().InsertAsync(newStop);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid stopId, UpdateStopModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var stop = await _unitOfWork.GetRepository<Stop>()
                .GetQueryable()
                .Include(s => s.RouteStops)
                .ThenInclude(rs => rs.Route)
                .FirstOrDefaultAsync(s => s.Id == stopId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng không tồn tại.");

            if (stop.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng đã bị xóa.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng ghi tên trạm.");

            stop.Name = model.Name;
            stop.LastUpdatedBy = userId;
            stop.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<Stop>().UpdateAsync(stop);

            // Cập nhật các route có liên quan
            if (stop.RouteStops != null && stop.RouteStops.Any(rs => !rs.DeletedTime.HasValue))
            {
                var routeIds = stop.RouteStops
                    .Where(rs => !rs.DeletedTime.HasValue)
                    .Select(rs => rs.RouteId)
                    .Distinct()
                    .ToList();

                foreach (var routeId in routeIds)
                {
                    await _routeService.UpdateRouteInformationAsync(routeId);
                }
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid stopId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var stop = await _unitOfWork.GetRepository<Stop>()
                .GetQueryable()
                .Include(x => x.RouteStops)
                .ThenInclude(rs => rs.Route)
                .FirstOrDefaultAsync(x => x.Id == stopId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng không tồn tại.");

            if (stop.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng đã bị xóa.");

            if (stop.RouteStops != null && stop.RouteStops.Any(rs => !rs.DeletedTime.HasValue))
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Không thể xóa trạm dừng đang được sử dụng trong tuyến.");

            stop.LastUpdatedTime = CoreHelper.SystemTimeNow;
            stop.LastUpdatedBy = userId;
            stop.DeletedTime = CoreHelper.SystemTimeNow;
            stop.DeletedBy = userId;

            await _unitOfWork.GetRepository<Stop>().UpdateAsync(stop);

            var routeIds = stop.RouteStops
                .Where(rs => !rs.DeletedTime.HasValue)
                .Select(rs => rs.RouteId)
                .Distinct()
                .ToList();

            await _unitOfWork.SaveAsync();

            foreach (var routeId in routeIds)
            {
                await _routeService.UpdateRouteInformationAsync(routeId);
            }
        }
    }
}

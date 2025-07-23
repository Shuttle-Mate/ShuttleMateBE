using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System.Net.Http.Json;

namespace ShuttleMate.Services.Services
{
    public class StopService : IStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly VietMapSettings _vietMapSettings;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public StopService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IOptions<VietMapSettings> vietMapSettings, HttpClient httpClient, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _vietMapSettings = vietMapSettings.Value;
            _httpClient = httpClient;
            _cache = cache;
        }

        private const string VietMapSearchApiUrl = "https://maps.vietmap.vn/api/search/demo";
        private const string VietMapPlaceApiUrl = "https://maps.vietmap.vn/api/place/demo";

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

        //public async Task<IEnumerable<ResponseSearchStopModel>> SearchAsync(string address)
        //{
        //    if (_cache.TryGetValue(address, out IEnumerable<ResponseSearchStopModel> cachedResult))
        //    {
        //        return cachedResult;
        //    }

        //    string apiUrl = $"{VietMapSearchApiUrl}?apikey={_vietMapSettings.ApiKey}&text={address}";
        //    var response = await _httpClient.GetFromJsonAsync<List<ResponseVietMapSearchModelcs>>(apiUrl);

        //    var result = response?
        //        .Select(x => new ResponseSearchStopModel
        //        {
        //            RefId = x.RefId,
        //            Address = x.Address
        //        })
        //        .ToList() ?? new List<ResponseSearchStopModel>();

        //    _cache.Set(address, result, TimeSpan.FromMinutes(10));

        //    return result;
        //}

        public async Task<ResponseStopModel> GetByIdAsync(Guid id)
        {
            var stop = await _unitOfWork.GetRepository<Stop>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng không tồn tại.");

            if (stop.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng đã bị xóa.");
            }

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

        public async Task UpdateAsync(Guid id, UpdateStopModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var stop = await _unitOfWork.GetRepository<Stop>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng không tồn tại.");

            if (stop.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng đã bị xóa.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng tên trạm.");

            stop.Name = model.Name;
            stop.LastUpdatedBy = userId;
            stop.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<Stop>().UpdateAsync(stop);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var stop = await _unitOfWork.GetRepository<Stop>()
                .GetQueryable()
                .Include(x => x.RouteStops)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng không tồn tại.");

            if (stop.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Trạm dừng đã bị xóa.");
            }

            if (stop.RouteStops != null && stop.RouteStops.Any())
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Không thể xóa trạm dừng đang được sử dụng trong tuyến.");
            }

            stop.LastUpdatedTime = CoreHelper.SystemTimeNow;
            stop.LastUpdatedBy = userId;
            stop.DeletedTime = CoreHelper.SystemTimeNow;
            stop.DeletedBy = userId;

            await _unitOfWork.GetRepository<Stop>().UpdateAsync(stop);
            await _unitOfWork.SaveAsync();
        }
    }
}

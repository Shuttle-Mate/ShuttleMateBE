using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.RecordModelViews;
using ShuttleMate.Services.Services.Infrastructure;

namespace ShuttleMate.Services.Services
{
    public class RecordService : IRecordService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public RecordService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<BasePaginatedList<ResponseRecordModel>> GetAllAsync(
            Guid? tripId,
            DateTime? from,
            DateTime? to,
            bool sortAsc = false,
            int page = 0,
            int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<ShuttleLocationRecord>()
                .GetQueryable()
                .Include(r => r.Trip)
                .Where(r => !r.DeletedTime.HasValue);

            if (tripId.HasValue)
                query = query.Where(r => r.TripId == tripId.Value);

            if (from.HasValue)
                query = query.Where(r => r.TimeStamp >= from.Value);

            if (to.HasValue)
                query = query.Where(r => r.TimeStamp <= to.Value);

            query = sortAsc
                ? query.OrderBy(r => r.TimeStamp)
                : query.OrderByDescending(r => r.TimeStamp);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ResponseRecordModel>>(pagedItems);

            return new BasePaginatedList<ResponseRecordModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseRecordModel> GetByIdAsync(Guid recordId)
        {
            var record = await _unitOfWork.GetRepository<ShuttleLocationRecord>().GetByIdAsync(recordId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Bản ghi vị trí không tồn tại.");

            if (record.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Bản ghi vị trí đã bị xóa.");

            return _mapper.Map<ResponseRecordModel>(record);
        }

        public async Task CreateAsync(CreateRecordModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            if (model.Lat == 0 || model.Lng == 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vị trí (kinh độ, vĩ độ) không hợp lệ.");
            }

            if (model.Accuracy <= 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Độ chính xác không hợp lệ.");
            }

            if (model.TimeStamp == default)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian không hợp lệ.");
            }

            if (model.TripId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "ID chuyến đi không hợp lệ.");
            }

            var tripExists = await _unitOfWork.GetRepository<Trip>().Entities.AnyAsync(t => t.Id == model.TripId && !t.DeletedTime.HasValue);
            if (!tripExists)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "ID chuyến đi không tồn tại.");
            }

            var newRecord = _mapper.Map<ShuttleLocationRecord>(model);
            newRecord.CreatedBy = userId;
            newRecord.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<ShuttleLocationRecord>().InsertAsync(newRecord);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid recordId, UpdateRecordModel model)
        {
            var record = await _unitOfWork.GetRepository<ShuttleLocationRecord>().GetByIdAsync(recordId)
        ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Bản ghi vị trí không tồn tại.");

            if (record.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Bản ghi vị trí đã bị xóa.");
            }

            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            if (model.Lat == 0 || model.Lng == 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vị trí (kinh độ, vĩ độ) không hợp lệ.");
            }

            if (model.Accuracy <= 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Độ chính xác không hợp lệ.");
            }

            if (model.TimeStamp == default)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian không hợp lệ.");
            }

            if (model.TripId == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "ID chuyến đi không hợp lệ.");
            }

            var tripExists = await _unitOfWork.GetRepository<Trip>().Entities.AnyAsync(t => t.Id == model.TripId && !t.DeletedTime.HasValue);
            if (!tripExists)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "ID chuyến đi không tồn tại.");
            }

            record = _mapper.Map<ShuttleLocationRecord>(model);

            await _unitOfWork.GetRepository<ShuttleLocationRecord>().UpdateAsync(record);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid recordId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid cb);

            var record = await _unitOfWork.GetRepository<ShuttleLocationRecord>().GetByIdAsync(recordId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Bản ghi vị trí không tồn tại.");

            if (record.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Bản ghi vị trí đã bị xóa.");
            }

            record.LastUpdatedTime = CoreHelper.SystemTimeNow;
            record.LastUpdatedBy = userId;
            record.DeletedTime = CoreHelper.SystemTimeNow;
            record.DeletedBy = userId;

            await _unitOfWork.GetRepository<ShuttleLocationRecord>().UpdateAsync(record);
            await _unitOfWork.SaveAsync();
        }
    }
}

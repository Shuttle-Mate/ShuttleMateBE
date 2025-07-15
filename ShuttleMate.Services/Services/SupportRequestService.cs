using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.ResponseSupportModelViews;
using ShuttleMate.ModelViews.SupportRequestModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public SupportRequestService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<BasePaginatedList<ResponseSupportRequestModel>> GetAllAsync(
            string? category,
            string? status,
            string? search,
            bool sortAsc = false,
            int page = 0,
            int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<SupportRequest>()
                .GetQueryable()
                .Include(x => x.User)
                .Where(x => !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<SupportRequestCategoryEnum>(category, true, out var parsedCategory))
            {
                query = query.Where(x => x.Category == parsedCategory);
            }
            
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SupportRequestStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(lowered) ||
                    x.Message.ToLower().Contains(lowered));
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ResponseSupportRequestModel>>(pagedItems);

            return new BasePaginatedList<ResponseSupportRequestModel>(result, totalCount, page, pageSize);
        }

        public async Task<IEnumerable<ResponseSupportRequestModel>> GetAllMyAsync(
            string? category,
            string? status,
            string? search,
            bool sortAsc = false)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var query = _unitOfWork.GetRepository<SupportRequest>()
                .GetQueryable()
                .Include(x => x.User)
                .Where(x => !x.DeletedTime.HasValue && x.UserId.ToString() == userId);

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<SupportRequestCategoryEnum>(category, true, out var parsedCategory))
            {
                query = query.Where(x => x.Category == parsedCategory);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SupportRequestStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(lowered) ||
                    x.Message.ToLower().Contains(lowered));
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var items = await query.ToListAsync();

            return _mapper.Map<IEnumerable<ResponseSupportRequestModel>>(items);
        }

        public async Task<IEnumerable<ResponseResponseSupportModel>> GetAllResponsesAsync(Guid id)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            var responseSupports = await _unitOfWork.GetRepository<ResponseSupport>()
                .GetQueryable()
                .Where(rs => rs.SupportRequestId == id && !rs.DeletedTime.HasValue)
                .OrderByDescending(rs => rs.CreatedTime)
                .ToListAsync();

            if (!responseSupports.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có phản hồi yêu cầu nào.");
            }

            return _mapper.Map<IEnumerable<ResponseResponseSupportModel>>(responseSupports);
        }

        public async Task<ResponseSupportRequestModel> GetByIdAsync(Guid id)
        {
            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            return _mapper.Map<ResponseSupportRequestModel>(supportRequest);
        }

        public async Task CreateAsync(CreateSupportRequestModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            if (!Enum.TryParse<SupportRequestCategoryEnum>(model.Category, true, out var categoryEnum) || !Enum.IsDefined(typeof(SupportRequestCategoryEnum), categoryEnum))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại yêu cầu hỗ trợ không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tiêu đề yêu cầu hỗ trợ.");
            }

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền nội dung yêu cầu hỗ trợ.");
            }

            var newSupportRequest = _mapper.Map<SupportRequest>(model);

            newSupportRequest.Category = categoryEnum;
            newSupportRequest.Status = SupportRequestStatusEnum.IN_PROGRESS;
            newSupportRequest.UserId = userIdGuid;
            newSupportRequest.CreatedBy = userId;
            newSupportRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<SupportRequest>().InsertAsync(newSupportRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task ResolveAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            supportRequest.Status = SupportRequestStatusEnum.RESOLVED;
            supportRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            supportRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<SupportRequest>().UpdateAsync(supportRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task CancelAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var supportRequest = await _unitOfWork.GetRepository<SupportRequest>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hỗ trợ không tồn tại.");

            supportRequest.Status = SupportRequestStatusEnum.CANCELLED;
            supportRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            supportRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<SupportRequest>().UpdateAsync(supportRequest);
            await _unitOfWork.SaveAsync();
        }
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.PromotionModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public PromotionService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<BasePaginatedList<ResponsePromotionModel>> GetAllAsync(
            string? search,
            string? type,
            bool? isExpired,
            DateTime? startEndDate,
            DateTime? endEndDate,
            Guid? userId,
            bool sortAsc = true,
            int page = 0,
            int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<Promotion>()
                .GetQueryable()
                .Include(p => p.Ticket)
                .Include(p => p.UserPromotions)
                .Where(p => !p.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.ToLower().Contains(search.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<TypePromotionEnum>(type, true, out var parsedType))
                query = query.Where(p => p.Type == parsedType);

            if (startEndDate.HasValue)
                query = query.Where(p => p.EndDate >= startEndDate.Value);

            if (endEndDate.HasValue)
                query = query.Where(p => p.EndDate <= endEndDate.Value);

            if (isExpired.HasValue)
                query = query.Where(p => p.IsExpiredOrReachLimit == isExpired.Value);

            if (userId.HasValue)
            {
                query = query.Where(p => p.UserPromotions.Any(up => up.UserId == userId.Value));
            }

            query = sortAsc
                ? query.OrderBy(p => p.EndDate)
                : query.OrderByDescending(p => p.EndDate);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ResponsePromotionModel>>(pagedItems);

            return new BasePaginatedList<ResponsePromotionModel>(result, totalCount, page, pageSize);
        }

        public async Task<IEnumerable<ResponsePromotionModel>> GetAllUnsavedAsync()
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            var guidUserId = Guid.Parse(userId);

            var promotions = await _unitOfWork.GetRepository<Promotion>().FindAllAsync(p => !p.DeletedTime.HasValue && p.EndDate > DateTime.UtcNow);

            var savedPromotionIds = (await _unitOfWork.GetRepository<UserPromotion>()
                .FindAllAsync(up => up.UserId == guidUserId))
                .Select(up => up.PromotionId)
                .ToHashSet();

            var unsavedPromotions = promotions
                .Where(p => !savedPromotionIds.Contains(p.Id))
                .ToList();

            if (!unsavedPromotions.Any())
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có khuyến mãi nào.");

            return _mapper.Map<IEnumerable<ResponsePromotionModel>>(unsavedPromotions);
        }

        public async Task<IEnumerable<ResponseUserPromotionModel>> GetAllUsersSavedAsync(Guid promotionId)
        {
            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(promotionId)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");

            var userPromotions = await _unitOfWork.GetRepository<UserPromotion>().FindAllAsync(up => up.PromotionId == promotionId);

            if (!userPromotions.Any())
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có người dùng nào lưu khuyến mãi.");

            var userIds = userPromotions.Select(up => up.UserId).Distinct().ToList();

            var users = await _unitOfWork.GetRepository<User>().FindAllAsync(u => userIds.Contains(u.Id));

            var result = from up in userPromotions
                         join u in users on up.UserId equals u.Id
                         select new ResponseUserPromotionModel
                         {
                             Id = u.Id,
                             FullName = u.FullName,
                             Email = u.Email,
                             IsUsed = up.IsUsed
                         };

            return result.ToList();
        }

        public async Task<ResponsePromotionModel> GetByIdAsync(Guid promotionId)
        {
            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(promotionId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");

            return _mapper.Map<ResponsePromotionModel>(promotion);
        }

        public async Task CreateAsync(CreatePromotionModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var ticket = await _unitOfWork.GetRepository<Ticket>().GetByIdAsync(model.TicketId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé không tồn tại.");

            if (ticket.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé đã bị xóa.");

            if (!Enum.TryParse<TypePromotionEnum>(model.Type, true, out var typeEnum) || !Enum.IsDefined(typeof(TypePromotionEnum), typeEnum))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên khuyến mãi.");

            if (string.IsNullOrWhiteSpace(model.Description))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");

            if (model.DiscountValue <= 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị khuyến mãi không hợp lệ.");

            if (model.LimitSalePrice < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giới hạn bán không hợp lệ.");

            if (model.EndDate <= DateTime.Now)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");

            if (model.UsingLimit < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");

            var newPromotion = _mapper.Map<Promotion>(model);
            newPromotion.Type = typeEnum;
            newPromotion.TicketId = model.TicketId;
            newPromotion.CreatedBy = userId;
            newPromotion.LastUpdatedBy = userId;

            switch (typeEnum)
            {
                case TypePromotionEnum.DIRECT_DISCOUNT:
                    newPromotion.DiscountPrice = model.DiscountValue;
                    break;
                case TypePromotionEnum.PERCENTAGE_DISCOUNT:
                    newPromotion.DiscountPercent = model.DiscountValue;
                    break;
                case TypePromotionEnum.FIXED_AMOUNT_DISCOUNT:
                    newPromotion.DiscountAmount = model.DiscountValue;
                    break;
                default:
                    break;
            }

            await _unitOfWork.GetRepository<Promotion>().InsertAsync(newPromotion);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid promotionId, UpdatePromotionModel model)
        {
            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(promotionId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");

            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var ticket = await _unitOfWork.GetRepository<Ticket>().GetByIdAsync(model.TicketId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé không tồn tại.");

            if (ticket.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé đã bị xóa.");

            if (!Enum.TryParse<TypePromotionEnum>(model.Type, true, out var typeEnum) || !Enum.IsDefined(typeof(TypePromotionEnum), typeEnum))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên khuyến mãi.");

            if (string.IsNullOrWhiteSpace(model.Description))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");

            if (model.DiscountValue <= 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị khuyến mãi không hợp lệ.");

            if (model.LimitSalePrice < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giới hạn bán không hợp lệ.");

            if (model.EndDate <= DateTime.Now)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");

            if (model.UsingLimit < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");

            _mapper.Map(model, promotion);
            promotion.Type = typeEnum;
            promotion.TicketId = model.TicketId;
            promotion.IsExpiredOrReachLimit = promotion.UsedCount >= model.UsingLimit;
            promotion.LastUpdatedBy = userId;
            promotion.LastUpdatedTime = CoreHelper.SystemTimeNow;
            promotion.DiscountPrice = null;
            promotion.DiscountPercent = null;
            promotion.DiscountAmount = null;

            switch (typeEnum)
            {
                case TypePromotionEnum.DIRECT_DISCOUNT:
                    promotion.DiscountPrice = model.DiscountValue;
                    break;
                case TypePromotionEnum.PERCENTAGE_DISCOUNT:
                    promotion.DiscountPercent = model.DiscountValue;
                    break;
                case TypePromotionEnum.FIXED_AMOUNT_DISCOUNT:
                    promotion.DiscountAmount = model.DiscountValue;
                    break;
            }

            await _unitOfWork.GetRepository<Promotion>().UpdateAsync(promotion);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid promotionId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(promotionId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");

            promotion.LastUpdatedTime = CoreHelper.SystemTimeNow;
            promotion.LastUpdatedBy = userId;
            promotion.DeletedTime = CoreHelper.SystemTimeNow;
            promotion.DeletedBy = userId;

            await _unitOfWork.GetRepository<Promotion>().UpdateAsync(promotion);
            await _unitOfWork.SaveAsync();
        }
    }
}

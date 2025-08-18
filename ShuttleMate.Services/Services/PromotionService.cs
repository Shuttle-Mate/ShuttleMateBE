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

        public async Task<IEnumerable<ResponsePromotionModel>> GetAllApplicableAsync(Guid ticketId)
        {
            var userId = Guid.Parse(Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor));

            var ticket = await _unitOfWork.GetRepository<Ticket>()
                .GetByIdAsync(ticketId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND, "Vé không tồn tại.");

            if (ticket.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND, "Vé đã bị xóa.");

            var applicablePromotions = await _unitOfWork.GetRepository<Promotion>().Entities
                .Where(p =>
                    !p.DeletedTime.HasValue &&
                    p.EndDate >= DateTime.UtcNow &&
                    !p.IsExpiredOrReachLimit &&
                    (p.UsingLimit == 0 || p.UsedCount < p.UsingLimit) &&
                    (
                        p.IsGlobal ||
                        (p.ApplicableTicketType != null && p.ApplicableTicketType == ticket.Type) ||
                        (p.TicketId != null && p.TicketId == ticket.Id)
                    )
                )
                .OrderBy(p => p.EndDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ResponsePromotionModel>>(applicablePromotions);
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

            if (!Enum.TryParse<TypePromotionEnum>(model.PromotionType, true, out var typeEnum)
                || !Enum.IsDefined(typeof(TypePromotionEnum), typeEnum))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên khuyến mãi.");

            if (string.IsNullOrWhiteSpace(model.Description))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");

            switch (typeEnum)
            {
                case TypePromotionEnum.PRICE_DISCOUNT:
                    if (!model.DiscountPrice.HasValue || model.DiscountPrice <= 0)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giảm giá không hợp lệ.");
                    break;

                case TypePromotionEnum.PERCENTAGE_DISCOUNT:
                    if (!model.DiscountPercent.HasValue || model.DiscountPercent <= 0 || model.DiscountPercent > 100)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phần trăm giảm giá không hợp lệ.");
                    break;
            }

            if (model.LimitSalePrice < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giới hạn bán không hợp lệ.");

            if (model.EndDate <= DateTime.Now)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");

            if (model.UsingLimit < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");

            bool isGlobal = false;
            TicketTypeEnum? applicableType = null;
            Guid? applicableTicketId = null;

            if (!string.IsNullOrWhiteSpace(model.TicketType))
            {
                if (!Enum.TryParse<TicketTypeEnum>(model.TicketType, true, out var parsedType)
                    || !Enum.IsDefined(typeof(TicketTypeEnum), parsedType))
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại vé không hợp lệ.");

                applicableType = parsedType;
                isGlobal = false;
            }
            else if (model.TicketId.HasValue)
            {
                var ticket = await _unitOfWork.GetRepository<Ticket>().GetByIdAsync(model.TicketId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé không tồn tại.");

                if (ticket.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé đã bị xóa.");

                applicableTicketId = ticket.Id;
                isGlobal = false;
            }
            else
            {
                isGlobal = true;
            }

            var newPromotion = new Promotion
            {
                Type = typeEnum,
                Name = model.Name,
                Description = model.Description,
                LimitSalePrice = model.LimitSalePrice,
                EndDate = model.EndDate,
                UsingLimit = model.UsingLimit,
                CreatedBy = userId,
                LastUpdatedBy = userId,
                ApplicableTicketType = applicableType,
                TicketId = applicableTicketId,
                IsGlobal = isGlobal
            };

            if (typeEnum == TypePromotionEnum.PRICE_DISCOUNT)
                newPromotion.DiscountPrice = model.DiscountPrice;
            else if (typeEnum == TypePromotionEnum.PERCENTAGE_DISCOUNT)
                newPromotion.DiscountPercent = model.DiscountPercent;

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

            if (!Enum.TryParse<TypePromotionEnum>(model.PromotionType, true, out var typeEnum) ||
                !Enum.IsDefined(typeof(TypePromotionEnum), typeEnum))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên khuyến mãi.");

            if (string.IsNullOrWhiteSpace(model.Description))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");

            switch (typeEnum)
            {
                case TypePromotionEnum.PRICE_DISCOUNT:
                    if (!model.DiscountPrice.HasValue || model.DiscountPrice <= 0)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giảm giá không hợp lệ.");
                    break;
                case TypePromotionEnum.PERCENTAGE_DISCOUNT:
                    if (!model.DiscountPercent.HasValue || model.DiscountPercent <= 0 || model.DiscountPercent > 100)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phần trăm giảm giá không hợp lệ.");
                    break;
            }

            if (model.LimitSalePrice < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giới hạn bán không hợp lệ.");

            if (model.EndDate <= DateTime.Now)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");

            if (model.UsingLimit < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");

            promotion.TicketId = null;
            promotion.ApplicableTicketType = null;
            promotion.IsGlobal = false;

            if (model.TicketId.HasValue)
            {
                var ticket = await _unitOfWork.GetRepository<Ticket>().GetByIdAsync(model.TicketId.Value)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé không tồn tại.");

                if (ticket.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vé đã bị xóa.");

                promotion.TicketId = ticket.Id;
            }
            else if (!string.IsNullOrWhiteSpace(model.TicketType))
            {
                if (!Enum.TryParse<TicketTypeEnum>(model.TicketType, true, out var ticketTypeEnum) ||
                    !Enum.IsDefined(typeof(TicketTypeEnum), ticketTypeEnum))
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại vé không hợp lệ.");

                promotion.ApplicableTicketType = ticketTypeEnum;
            }
            else
            {
                promotion.IsGlobal = true;
            }

            promotion.Type = typeEnum;
            promotion.Name = model.Name;
            promotion.Description = model.Description;
            promotion.UsingLimit = model.UsingLimit;
            promotion.EndDate = model.EndDate;
            promotion.LimitSalePrice = model.LimitSalePrice;
            promotion.IsExpiredOrReachLimit = promotion.UsedCount >= model.UsingLimit;
            promotion.LastUpdatedBy = userId;
            promotion.LastUpdatedTime = CoreHelper.SystemTimeNow;

            promotion.DiscountPrice = null;
            promotion.DiscountPercent = null;

            if (typeEnum == TypePromotionEnum.PRICE_DISCOUNT)
                promotion.DiscountPrice = model.DiscountPrice;
            else if (typeEnum == TypePromotionEnum.PERCENTAGE_DISCOUNT)
                promotion.DiscountPercent = model.DiscountPercent;

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

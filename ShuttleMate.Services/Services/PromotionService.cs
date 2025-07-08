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

        public async Task<IEnumerable<ResponsePromotionModel>> GetAllAsync()
        {
            var promotions = await _unitOfWork.GetRepository<Promotion>().FindAllAsync(a => !a.DeletedTime.HasValue);

            if (!promotions.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có khuyến mãi nào.");
            }

            return _mapper.Map<IEnumerable<ResponsePromotionModel>>(promotions);
        }

        public async Task<IEnumerable<ResponsePromotionModel>> GetAllMyAsync()
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            var guidUserId = Guid.Parse(userId);

            var allPromotions = await _unitOfWork.GetRepository<Promotion>()
                .FindAllAsync(p => !p.DeletedTime.HasValue && p.UserPromotions.Any(up => up.UserId == guidUserId && p.EndDate > DateTime.UtcNow));

            if (!allPromotions.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Bạn không có khuyến mãi nào.");
            }

            return _mapper.Map<IEnumerable<ResponsePromotionModel>>(allPromotions);
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

            return _mapper.Map<IEnumerable<ResponsePromotionModel>>(unsavedPromotions);
        }

        public async Task<IEnumerable<ResponseUserPromotionModel>> GetAllUsersSavedAsync(Guid id)
        {
            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(id)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");
            }

            var userPromotions = await _unitOfWork.GetRepository<UserPromotion>().FindAllAsync(up => up.PromotionId == id);

            if (!userPromotions.Any())
                return Enumerable.Empty<ResponseUserPromotionModel>();

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

        public async Task<ResponsePromotionModel> GetByIdAsync(Guid id)
        {
            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");
            }

            return _mapper.Map<ResponsePromotionModel>(promotion);
        }

        public async Task CreateAsync(CreatePromotionModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            if (model.TicketTypeIds == null || !model.TicketTypeIds.Any())
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn ít nhất một loại vé.");
            }

            var ticketTypes = await _unitOfWork.GetRepository<TicketType>().FindAllAsync(x => model.TicketTypeIds.Contains(x.Id));

            if (ticketTypes.Count != model.TicketTypeIds.Count)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Một hoặc nhiều loại vé không tồn tại.");
            }

            if (ticketTypes.Any(t => t.DeletedTime.HasValue))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Một hoặc nhiều loại vé đã bị xóa.");
            }

            if (!Enum.TryParse<TypePromotionEnum>(model.Type, true, out var typeEnum) || !Enum.IsDefined(typeof(TypePromotionEnum), typeEnum))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên khuyến mãi.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");
            }

            if (model.EndDate <= DateTime.Now)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");
            }

            if (model.UsingLimit < 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");
            }

            var newPromotion = _mapper.Map<Promotion>(model);
            newPromotion.Type = typeEnum;
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

            foreach (var ticketTypeId in model.TicketTypeIds)
            {
                var ticketPromotion = new TicketPromotion
                {
                    TicketId = ticketTypeId,
                    PromotionId = newPromotion.Id,
                    CreatedBy = userId,
                    LastUpdatedBy = userId
                };

                await _unitOfWork.GetRepository<TicketPromotion>().InsertAsync(ticketPromotion);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid id, UpdatePromotionModel model)
        {
            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");
            }

            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            if (string.IsNullOrWhiteSpace(model.Name))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên khuyến mãi.");

            if (string.IsNullOrWhiteSpace(model.Description))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");

            if (!Enum.TryParse<TypePromotionEnum>(model.Type, true, out var typeEnum) || !Enum.IsDefined(typeof(TypePromotionEnum), typeEnum))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");

            if (model.DiscountValue <= 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị khuyến mãi không hợp lệ.");

            if (model.LimitSalePrice <= 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giới hạn bán không hợp lệ.");

            if (model.EndDate <= DateTime.Now)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");

            if (model.UsingLimit < 0)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");

            if (model.TicketTypeIds == null || !model.TicketTypeIds.Any())
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn ít nhất một loại vé.");

            var ticketTypes = await _unitOfWork.GetRepository<TicketType>().FindAllAsync(x => model.TicketTypeIds.Contains(x.Id));

            if (ticketTypes.Count != model.TicketTypeIds.Count)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Một hoặc nhiều loại vé không tồn tại.");

            if (ticketTypes.Any(t => t.DeletedTime.HasValue))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Một hoặc nhiều loại vé đã bị xóa.");

            promotion.Name = model.Name;
            promotion.Description = model.Description;
            promotion.Type = typeEnum;
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

            promotion.LimitSalePrice = model.LimitSalePrice;
            promotion.EndDate = model.EndDate;
            promotion.UsingLimit = model.UsingLimit;
            promotion.IsExpiredOrReachLimit = model.EndDate < DateTime.Now || promotion.UsedCount >= model.UsingLimit;
            promotion.LastUpdatedBy = userId;
            promotion.LastUpdatedTime = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Promotion>().UpdateAsync(promotion);

            var oldTicketPromotions = await _unitOfWork.GetRepository<TicketPromotion>().FindAllAsync(x => x.PromotionId == promotion.Id);
            foreach (var old in oldTicketPromotions)
            {
                await _unitOfWork.GetRepository<TicketPromotion>().DeleteAsync(old);
            }

            foreach (var ticketTypeId in model.TicketTypeIds)
            {
                var newTicketPromotion = new TicketPromotion
                {
                    PromotionId = promotion.Id,
                    TicketId = ticketTypeId,
                    CreatedBy = userId,
                    LastUpdatedBy = userId
                };

                await _unitOfWork.GetRepository<TicketPromotion>().InsertAsync(newTicketPromotion);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");
            }

            promotion.LastUpdatedTime = CoreHelper.SystemTimeNow;
            promotion.LastUpdatedBy = userId;
            promotion.DeletedTime = CoreHelper.SystemTimeNow;
            promotion.DeletedBy = userId;

            await _unitOfWork.GetRepository<Promotion>().UpdateAsync(promotion);
            await _unitOfWork.SaveAsync();
        }

        public async Task SavePromotionAsync(Guid id)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);

            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");
            }

            var isSaved = await _unitOfWork.GetRepository<UserPromotion>().Entities.AnyAsync(up => up.UserId == userIdGuid && up.PromotionId == id);

            if (isSaved)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Khuyến mãi đã được lưu.");
            }

            var userPromotion = new UserPromotion
            {
                UserId = userIdGuid,
                PromotionId = id
            };

            await _unitOfWork.GetRepository<UserPromotion>().InsertAsync(userPromotion);
            await _unitOfWork.SaveAsync();
        }
    }
}

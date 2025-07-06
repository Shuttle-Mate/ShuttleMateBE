using AutoMapper;
using Microsoft.AspNetCore.Http;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.PromotionModelViews;
using ShuttleMate.ModelViews.SupportRequestModelViews;
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

        public async Task<IEnumerable<ResponsePromotionModel>> GetAllAdminAsync()
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

            var myPromotions = await _unitOfWork.GetRepository<Promotion>().FindAllAsync(a => !a.DeletedTime.HasValue && a.UserId.ToString() == userId);

            if (!myPromotions.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có khuyến mãi nào.");
            }

            return _mapper.Map<IEnumerable<ResponsePromotionModel>>(myPromotions);
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

            var ticketType = await _unitOfWork.GetRepository<TicketType>().GetByIdAsync(model.TicketTypeId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại.");

            if (ticketType.DeletedTime.HasValue)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé này đã bị xóa.");
            }

            if (!Enum.IsDefined(typeof(TypePromotionEnum), model.Type))
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

            newPromotion.CreatedBy = userId;
            newPromotion.LastUpdatedBy = userId;

            switch (model.Type)
            {
                case TypePromotionEnum.FixedAmountDiscount:
                    newPromotion.DiscountPrice = model.DiscountValue;
                    break;
                case TypePromotionEnum.PercentageDiscount:
                    newPromotion.DiscountPercent = model.DiscountValue;
                    break;
                //case TypePromotionEnum.DiscountAmount:
                //    // Gán giá trị giảm giá cho loại "Giảm theo lượng tiền"
                //    promotion.DiscountAmount = model.DiscountValue;
                //    break;
                default:
                    break;
            }

            //var newTicketPromotion = new TicketPromotion();
            //newTicketPromotion.CreatedBy = userId;
            //newTicketPromotion.LastUpdatedBy = userId;
            //newTicketPromotion.TicketId = ticketType.Id;
            //newTicketPromotion.PromotionId = newPromotion.Id;

            await _unitOfWork.GetRepository<Promotion>().InsertAsync(newPromotion);
            await _unitOfWork.SaveAsync();
        }

        //public async Task CreateAsync(CreatePromotionModel model)
        //{
        //    var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
        //    Guid.TryParse(userId, out Guid userIdGuid);
        //    model.TrimAllStrings();

        //    var ticketType = await _unitOfWork.GetRepository<TicketType>().GetByIdAsync(model.TicketTypeId)
        //        ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại.");

        //    if (ticketType.DeletedTime.HasValue)
        //    {
        //        throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé này đã bị xóa.");
        //    }

        //    if (string.IsNullOrWhiteSpace(model.Name))
        //    {
        //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên khuyến mãi.");
        //    }

        //    if (string.IsNullOrWhiteSpace(model.Description))
        //    {
        //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");
        //    }

        //    if (model.DiscountValue <= 0)
        //    {
        //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá giảm không hợp lệ.");
        //    }

        //    if (model.LimitSalePrice <= 0)
        //    {
        //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giới hạn bán không hợp lệ.");
        //    }

        //    if (model.EndDate <= DateTime.Now)
        //    {
        //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");
        //    }

        //    if (model.UsingLimit < 0)
        //    {
        //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");
        //    }

        //    if (!Enum.IsDefined(typeof(TypePromotionEnum), model.Type))
        //    {
        //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");
        //    }

        //    var newPromotion = _mapper.Map<Promotion>(model);

        //    newPromotion.CreatedBy = userId;
        //    newPromotion.LastUpdatedBy = userId;
        //    newPromotion.IsExpiredOrReachLimit = false;

        //    await _unitOfWork.GetRepository<Promotion>().InsertAsync(newPromotion);
        //    await _unitOfWork.SaveAsync();
        //}

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

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền mô tả khuyến mãi.");
            }

            if (model.DiscountPrice <= 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá giảm không hợp lệ.");
            }

            if (model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phần trăm giảm phải nằm trong khoảng từ 0 đến 100.");
            }

            if (model.LimitSalePrice <= 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giá trị giới hạn bán không hợp lệ.");
            }

            if (model.EndDate <= DateTime.Now)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Ngày kết thúc khuyến mãi không hợp lệ.");
            }

            if (model.UsingLimit < 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giới hạn sử dụng không hợp lệ.");
            }

            if (!Enum.IsDefined(typeof(TypePromotionEnum), model.Type))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phân loại khuyến mãi không hợp lệ.");
            }

            promotion.Description = model.Description;
            promotion.DiscountPrice = model.DiscountPrice;
            promotion.DiscountPercent = model.DiscountPercent;
            promotion.LimitSalePrice = model.LimitSalePrice;
            promotion.EndDate = model.EndDate;
            promotion.UsingLimit = model.UsingLimit;
            promotion.UsedCount = model.UsedCount;
            promotion.IsExpiredOrReachLimit = model.EndDate < DateTime.Now || model.UsedCount >= model.UsingLimit;
            promotion.Name = model.Name;
            promotion.Type = model.Type;
            promotion.LastUpdatedBy = userId;
            promotion.LastUpdatedTime = DateTime.UtcNow;

            await _unitOfWork.GetRepository<Promotion>().UpdateAsync(promotion);
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

            if (promotion.UserId.HasValue && promotion.UserId == guidUserId)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã được lưu.");
            }

            promotion.UserId = guidUserId;

            await _unitOfWork.GetRepository<Promotion>().UpdateAsync(promotion);
            await _unitOfWork.SaveAsync();
        }
    }
}

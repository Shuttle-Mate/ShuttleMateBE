using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.UserPromotionModelViews;

namespace ShuttleMate.Services.Services
{
    public class UserPromotionService : IUserPromotionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserPromotionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        public async Task CreateAsync(CreateUserPromotionModel model)
        {
            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(model.UserId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại.");

            if (user.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng đã bị xóa.");

            if (user.Violate == true)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng đã bị khóa.");

            var promotion = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(model.PromotionId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi không tồn tại.");

            if (promotion.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Khuyến mãi đã bị xóa.");

            var isSaved = await _unitOfWork.GetRepository<UserPromotion>().Entities.AnyAsync(up => up.UserId == model.UserId && up.PromotionId == model.PromotionId);

            if (isSaved)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Khuyến mãi đã được lưu.");
            }

            var userPromotion = new UserPromotion
            {
                UserId = model.UserId,
                PromotionId = model.PromotionId
            };

            await _unitOfWork.GetRepository<UserPromotion>().InsertAsync(userPromotion);
            await _unitOfWork.SaveAsync();
        }
    }
}

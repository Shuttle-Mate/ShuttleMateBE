using ShuttleMate.ModelViews.UserPromotionModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IUserPromotionService
    {
        Task CreateAsync(CreateUserPromotionModel model);
    }
}

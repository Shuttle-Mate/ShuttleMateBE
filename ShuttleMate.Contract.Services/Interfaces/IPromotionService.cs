using ShuttleMate.ModelViews.PromotionModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IPromotionService
    {
        Task<IEnumerable<ResponsePromotionModel>> GetAllAsync();
        Task<IEnumerable<ResponsePromotionModel>> GetAllMyAsync();
        Task<IEnumerable<ResponsePromotionModel>> GetAllUnsavedAsync();
        Task<IEnumerable<ResponseUserPromotionModel>> GetAllUsersSavedAsync(Guid id);
        Task<ResponsePromotionModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreatePromotionModel model);
        Task UpdateAsync(Guid id, UpdatePromotionModel model);
        Task DeleteAsync(Guid id);
        Task SavePromotionAsync(Guid id);
    }
}

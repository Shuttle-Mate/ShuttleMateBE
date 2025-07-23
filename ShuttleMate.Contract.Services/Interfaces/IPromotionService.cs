using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.PromotionModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IPromotionService
    {
        Task<BasePaginatedList<ResponsePromotionModel>> GetAllAsync(string? search, string? type, bool? isExpired, DateTime? startEndDate, DateTime? endEndDate, bool sortAsc = false, int page = 0, int pageSize = 10);
        Task<BasePaginatedList<ResponsePromotionModel>> GetAllMyAsync(string? search, string? type, bool? isExpired, DateTime? startEndDate, DateTime? endEndDate, bool sortAsc = false, int page = 0, int pageSize = 10);
        Task<IEnumerable<ResponsePromotionModel>> GetAllUnsavedAsync();
        Task<IEnumerable<ResponseUserPromotionModel>> GetAllUsersSavedAsync(Guid id);
        Task<ResponsePromotionModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreatePromotionModel model);
        Task UpdateAsync(Guid id, UpdatePromotionModel model);
        Task DeleteAsync(Guid id);
        Task SavePromotionAsync(Guid id);
    }
}

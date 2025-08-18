using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.PromotionModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IPromotionService
    {
        Task<BasePaginatedList<ResponsePromotionModel>> GetAllAsync(string? search, string? type, bool? isExpired, DateTime? startEndDate, DateTime? endEndDate, Guid? userId, bool sortAsc = true, int page = 0, int pageSize = 10);
        Task<IEnumerable<ResponsePromotionModel>> GetAllApplicableAsync(Guid ticketId);
        Task<ResponsePromotionModel> GetByIdAsync(Guid promotionIdid);
        Task CreateAsync(CreatePromotionModel model);
        Task UpdateAsync(Guid promotionId, UpdatePromotionModel model);
        Task DeleteAsync(Guid ipromotionIdd);
    }
}

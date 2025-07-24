using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.WithdrawalRequestModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IWithdrawalRequestService
    {
        Task<BasePaginatedList<ResponseWithdrawalRequestModel>> GetAllAsync(string? status, bool sortAsc = false, int page = 0, int pageSize = 10);
        Task<BasePaginatedList<ResponseWithdrawalRequestModel>> GetAllMyAsync(string? status, bool sortAsc = false, int page = 0, int pageSize = 10);
        Task<ResponseWithdrawalRequestModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateWithdrawalRequestModel model);
        Task UpdateAsync(Guid id, UpdateWithdrawalRequestModel model);
        Task CompleteAsync(Guid id);
        Task RejectAsync(Guid id, RejectWithdrawalRequestModel model);
        Task DeleteAsync(Guid id);
    }
}

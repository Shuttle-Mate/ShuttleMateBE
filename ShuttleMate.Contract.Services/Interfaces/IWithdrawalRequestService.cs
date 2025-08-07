using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.WithdrawalRequestModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IWithdrawalRequestService
    {
        Task<BasePaginatedList<ResponseWithdrawalRequestModel>> GetAllAsync(string? status, Guid? userId, bool sortAsc = false, int page = 0, int pageSize = 10);
        Task<ResponseWithdrawalRequestModel> GetByIdAsync(Guid withdrawalRequestId);
        Task CreateAsync(CreateWithdrawalRequestModel model);
        Task UpdateAsync(Guid withdrawalRequestId, UpdateWithdrawalRequestModel model);
        Task CompleteAsync(Guid withdrawalRequestId);
        Task RejectAsync(Guid withdrawalRequestId, RejectWithdrawalRequestModel model);
        Task DeleteAsync(Guid withdrawalRequestId);
    }
}

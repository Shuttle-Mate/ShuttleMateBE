using ShuttleMate.ModelViews.SupportRequestModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ISupportRequestService
    {
        Task<IEnumerable<ResponseSupportRequestModel>> GetAllAdminAsync();
        Task<IEnumerable<ResponseSupportRequestModel>> GetAllMyAsync();
        Task<ResponseSupportRequestModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateSupportRequestModel model);
        Task ChangeStatusAsync(Guid id, UpdateSupportRequestModel model);
        Task DeleteAsync(Guid id);
    }
}

using ShuttleMate.ModelViews.ResponseSupportModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IResponseSupportService
    {
        Task<ResponseResponseSupportModel> GetByIdAsync(Guid id);
        Task CreateAsync(CreateResponseSupportModel model);
        Task DeleteAsync(Guid id);
    }
}

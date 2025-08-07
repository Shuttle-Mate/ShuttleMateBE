using ShuttleMate.ModelViews.ResponseSupportModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IResponseSupportService
    {
        Task CreateAsync(CreateResponseSupportModel model);
        Task DeleteAsync(Guid responseSupportId);
    }
}

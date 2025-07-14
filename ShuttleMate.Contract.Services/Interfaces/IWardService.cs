using ShuttleMate.ModelViews.WardModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IWardService
    {
        Task<IEnumerable<ResponseWardModel>> GetAllAsync();
    }
}

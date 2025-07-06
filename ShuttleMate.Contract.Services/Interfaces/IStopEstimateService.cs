using ShuttleMate.ModelViews.StopEstimateModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IStopEstimateService
    {
        Task<IEnumerable<ResponseStopEstimateModel>> GetAllAsync();
        Task<IEnumerable<ResponseStopEstimateModel>> GetByRouteIdAsync(Guid routeId);
        Task CreateAsync(Guid routeId);
    }
}

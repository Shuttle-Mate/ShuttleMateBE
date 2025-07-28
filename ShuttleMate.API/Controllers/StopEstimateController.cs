using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.StopEstimateModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/stop-estimate")]
    [ApiController]
    public class StopEstimateController : ControllerBase
    {
        private readonly IStopEstimateService _stopEstimateService;

        public StopEstimateController(IStopEstimateService stopEstimateService)
        {
            _stopEstimateService = stopEstimateService;
        }
    }
}

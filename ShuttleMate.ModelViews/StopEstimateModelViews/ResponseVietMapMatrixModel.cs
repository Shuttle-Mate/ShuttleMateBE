using Newtonsoft.Json;

namespace ShuttleMate.ModelViews.StopEstimateModelViews
{
    public class ResponseVietMapMatrixModel
    {
        [JsonProperty("durations")]
        public List<List<double>> Durations { get; set; }
    }
}

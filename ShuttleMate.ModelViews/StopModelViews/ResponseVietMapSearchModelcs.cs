using System.Text.Json.Serialization;

namespace ShuttleMate.ModelViews.StopModelViews
{
    public class ResponseVietMapSearchModelcs
    {
        [JsonPropertyName("ref_id")]
        public string RefId { get; set; }

        [JsonPropertyName("display")]
        public string Address { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace ShuttleMate.ModelViews.StopModelViews
{
    public class ResponseVietMapPlaceModel
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }

        [JsonPropertyName("display")]
        public string Address { get; set; }
        [JsonPropertyName("ward")]
        public string WardName { get; set; }
    }
}

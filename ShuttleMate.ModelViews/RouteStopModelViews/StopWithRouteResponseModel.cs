namespace ShuttleMate.ModelViews.RouteStopModelViews
{
    public class StopWithRouteResponseModel
    {
        public Guid StopId { get; set; }
        public string StopName { get; set; }
        public string Address { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public List<RouteResponseModel> Routes { get; set; } = new();
    }
}

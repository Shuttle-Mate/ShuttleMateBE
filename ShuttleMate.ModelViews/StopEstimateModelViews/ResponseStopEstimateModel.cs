namespace ShuttleMate.ModelViews.StopEstimateModelViews
{
    public class ResponseStopEstimateModel
    {
        public Guid Id { get; set; }
        public Guid StopId { get; set; }
        public string StopName { get; set; }
        public Guid DepartureTimeId { get; set; }
        public TimeOnly ExpectedTime { get; set; }
    }
}

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class ResponseTripLocationModel
    {
        public double DistanceMeters { get; set; }
        public double DurationSeconds { get; set; }
        public string NextStopName { get; set; }
        public string Message { get; set; }
    }
}

namespace ShuttleMate.ModelViews.DepartureTimeModelViews
{
    public class ResponseDepartureTimeModel
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public TimeOnly Departure { get; set; }
        public string DayOfWeek { get; set; }
    }
}

namespace ShuttleMate.ModelViews.DepartureTimeModelViews
{
    public class CreateDepartureTimeModel
    {
        public Guid RouteId { get; set; }
        public string Departure { get; set; }
        public string DayOfWeek { get; set; }
    }
}

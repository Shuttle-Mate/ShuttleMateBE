namespace ShuttleMate.ModelViews.DepartureTimeModelViews
{
    public class CreateDepartureTimeModel
    {
        public Guid RouteId { get; set; }
        public List<CreateDepartureTimeDetailModel> DepartureTimes { get; set; } = new();
    }

    public class CreateDepartureTimeDetailModel
    {
        public string Time { get; set; }
        public string DayOfWeek { get; set; }
    }
}

namespace ShuttleMate.ModelViews.DepartureTimeModelViews
{
    public class UpdateDepartureTimeModel
    {
        public Guid RouteId { get; set; }
        public List<UpdateDepartureTimeDetailModel> DepartureTimes { get; set; } = new();
    }

    public class UpdateDepartureTimeDetailModel
    {
        public string Time { get; set; }
        public string DayOfWeek { get; set; }
    }
}

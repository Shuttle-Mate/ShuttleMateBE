namespace ShuttleMate.ModelViews.ScheduleModelViews
{
    public class UpdateScheduleModel
    {
        public Guid RouteId { get; set; }
        public Guid ShuttleId { get; set; }
        public Guid DriverId { get; set; }
        public string Direction { get; set; }
        public List<UpdateDepartureTimeModel> DepartureTimes { get; set; } = new();
    }

    public class UpdateDepartureTimeModel
    {
        public string Time { get; set; }
        public string DayOfWeek { get; set; }
    }
}

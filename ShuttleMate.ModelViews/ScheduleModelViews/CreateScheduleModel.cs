namespace ShuttleMate.ModelViews.ScheduleModelViews
{
    public class CreateScheduleModel
    {
        public Guid RouteId { get; set; }
        public List<CreateScheduleDetailModel> Schedules { get; set; } = new();
    }

    public class CreateScheduleDetailModel
    {
        public Guid ShuttleId { get; set; }
        public Guid DriverId { get; set; }
        public List<CreateDepartureTimeModel> DepartureTimes { get; set; } = new();
    }

    public class CreateDepartureTimeModel
    {
        public string Time { get; set; }
        public string DayOfWeek { get; set; }
    }
}

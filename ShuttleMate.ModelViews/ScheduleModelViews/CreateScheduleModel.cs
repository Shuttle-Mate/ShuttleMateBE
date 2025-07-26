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
        public Guid SchoolShiftId { get; set; }
        public string DepartureTime { get; set; }
        public List<CreateDayOfWeekModel> DayOfWeeks { get; set; } = new();
    }

    public class CreateDayOfWeekModel
    {
        public string DayOfWeek { get; set; }
    }
}

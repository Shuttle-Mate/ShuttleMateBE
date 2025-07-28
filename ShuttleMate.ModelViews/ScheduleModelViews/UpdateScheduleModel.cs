namespace ShuttleMate.ModelViews.ScheduleModelViews
{
    public class UpdateScheduleModel
    {
        public Guid ShuttleId { get; set; }
        public Guid DriverId { get; set; }
        public Guid SchoolShiftId { get; set; }
        public string DepartureTime { get; set; }
        public List<UpdateDayOfWeekModel> DayOfWeeks { get; set; } = new();
    }

    public class UpdateDayOfWeekModel
    {
        public string DayOfWeek { get; set; }
    }
}

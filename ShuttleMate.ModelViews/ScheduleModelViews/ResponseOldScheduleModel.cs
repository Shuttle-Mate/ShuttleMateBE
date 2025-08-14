namespace ShuttleMate.ModelViews.ScheduleModelViews
{
    public class ResponseOldScheduleModel
    {
        public Guid Id { get; set; }
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public string DepartureTime { get; set; }
        public string Direction { get; set; }
        public ResponseShuttleScheduleModel Shuttle { get; set; }
        public ResponseDriverScheduleModel Driver { get; set; }
        public ResponseSchoolShiftScheduleModel SchoolShift { get; set; }
        public List<ResponseDayOfWeekModel> DayOfWeeks { get; set; } = new();
    }

    public class ResponseDayOfWeekModel
    {
        public string DayOfWeek { get; set; }
    }
}

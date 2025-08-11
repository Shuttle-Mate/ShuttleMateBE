namespace ShuttleMate.ModelViews.ScheduleModelViews
{
    public class ResponseScheduleModel
    {
        public string DayOfWeek { get; set; }
        public List<ResponseScheduleDetailModel> Schedules { get; set; } = new();
    }

    public class ResponseScheduleDetailModel
    {
        public Guid Id { get; set; }
        public string DepartureTime { get; set; }
        public string Direction { get; set; }
        public ResponseShuttleScheduleModel Shuttle { get; set; }
        public ResponseDriverScheduleModel Driver { get; set; }
        public ResponseSchoolShiftScheduleModel SchoolShift { get; set; }
    }

    public class ResponseShuttleScheduleModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class ResponseDriverScheduleModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
    }

    public class ResponseSchoolShiftScheduleModel
    {
        public Guid Id { get; set; }
        public string ShiftType { get; set; }
        public string SessionType { get; set; }
    }
}

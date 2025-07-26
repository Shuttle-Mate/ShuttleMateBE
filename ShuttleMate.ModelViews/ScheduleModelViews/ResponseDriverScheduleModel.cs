namespace ShuttleMate.ModelViews.ScheduleModelViews
{
    public class ResponseTodayScheduleForDriverModel
    {
        public Guid Id { get; set; }
        public string RouteCode { get; set; }
        public string RouteName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string LicensePlate { get; set; }
        public int AttendedStudentCount { get; set; }
        public int ExpectedStudentCount { get; set; }
        public string EstimatedDuration { get; set; }
        public string Direction { get; set; }
        public ResponseRouteScheduleForDriverModel Route { get; set; }
    }

    public class ResponseRouteScheduleForDriverModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public int StopsCount { get; set; }
    }
}

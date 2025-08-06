namespace ShuttleMate.ModelViews.ScheduleModelViews
{
    public class ResponseTodayScheduleForDriverModel
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public Guid SchoolShiftId { get; set; }
        public string RouteCode { get; set; }
        public string RouteName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ShuttleName { get; set; }
        public string LicensePlate { get; set; }
        public string VehicleType { get; set; }
        public string Color { get; set; }
        public int SeatCount { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime InspectionDate { get; set; }
        public DateTime NextInspectionDate { get; set; }
        public DateTime InsuranceExpiryDate { get; set; }
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

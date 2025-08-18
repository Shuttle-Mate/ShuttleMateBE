namespace ShuttleMate.ModelViews.ScheduleOverrideModelView
{
    public class CreateScheduleOverrideModel
    {
        public DateOnly Date { get; set; }
        public Guid ScheduleId { get; set; }
        public string? ShuttleReason { get; set; }
        public Guid? OverrideShuttleId { get; set; }
        public string? DriverReason { get; set; }
        public Guid? OverrideUserId { get; set; }
    }
}

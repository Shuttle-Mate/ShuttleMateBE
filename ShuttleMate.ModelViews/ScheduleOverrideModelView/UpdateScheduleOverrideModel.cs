namespace ShuttleMate.ModelViews.ScheduleOverrideModelView
{
    public class UpdateScheduleOverrideModel
    {
        public string? ShuttleReason { get; set; }
        public Guid? OverrideShuttleId { get; set; }
        public string? DriverReason { get; set; }
        public Guid? OverrideUserId { get; set; }
    }
}

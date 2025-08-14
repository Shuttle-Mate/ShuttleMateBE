namespace ShuttleMate.ModelViews.ScheduleOverrideModelView
{
    public class UpdateScheduleOverrideModel
    {
        public Guid? OverrideShuttleId { get; set; }
        public Guid? OverrideUserId { get; set; }
        public string? Reason { get; set; }
    }
}

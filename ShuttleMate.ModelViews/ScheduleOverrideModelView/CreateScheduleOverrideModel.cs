namespace ShuttleMate.ModelViews.ScheduleOverrideModelView
{
    public class CreateScheduleOverrideModel
    {
        public DateOnly Date { get; set; }
        public string? Reason { get; set; }
        public Guid ScheduleId { get; set; }
        public Guid? OverrideShuttleId { get; set; }
        public Guid? OverrideUserId { get; set; }
    }
}

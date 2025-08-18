using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.ModelViews.ScheduleOverrideModelView
{
    public class ResponseScheduleOverrideModel
    {
        public Guid Id { get; set; }
        public string? ShuttleReason { get; set; }
        public ResponseShuttleScheduleModel? OverrideShuttle { get; set; }
        public string? DriverReason { get; set; }
        public ResponseDriverScheduleModel? OverrideDriver { get; set; }
    }
}

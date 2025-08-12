using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class ResponseTripModel
    {
        public Guid Id { get; set; }
        public DateOnly TripDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public TripStatusEnum Status { get; set; }
        public Guid ScheduleId { get; set; }
    }
}
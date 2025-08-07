using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.FeedbackModelViews
{
    public class CreateFeedbackModel
    {
        public string FeedbackCategory { get; set; }
        public string Message { get; set; }
        public int Rating { get; set; }
        public Guid TripId { get; set; }
    }
}

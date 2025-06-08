using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.FeedbackModelViews
{
    public class ResponseFeedbackModel
    {
        public Guid Id { get; set; }
        public string FeedbackCategory { get; set; }
        public string Message { get; set; }
        public int Rating { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
    }
}

using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.FeedbackModelViews
{
    public class CreateFeedbackModel
    {
        public FeedbackCategoryEnum FeedbackCategory { get; set; }
        public string Message { get; set; }
        public int Rating { get; set; }
    }
}

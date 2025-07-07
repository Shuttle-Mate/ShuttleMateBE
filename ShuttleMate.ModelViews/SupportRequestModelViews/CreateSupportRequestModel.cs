using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.SupportRequestModelViews
{
    public class CreateSupportRequestModel
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
}

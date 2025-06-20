namespace ShuttleMate.ModelViews.SupportRequestModelViews
{
    public class ResponseSupportRequestModel
    {
        public Guid Id { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public Guid UserId { get; set; }
    }
}

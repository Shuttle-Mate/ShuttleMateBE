namespace ShuttleMate.ModelViews.RecordModelViews
{
    public class CreateRecordModel
    {
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal Accuracy { get; set; }
        public Guid TripId { get; set; }
    }
}

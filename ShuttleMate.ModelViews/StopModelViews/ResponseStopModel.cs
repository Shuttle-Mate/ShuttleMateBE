namespace ShuttleMate.ModelViews.StopModelViews
{
    public class ResponseStopModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public Guid WardId { get; set; }
        public string WardName { get; set; }
    }

    public class BasicStopModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}

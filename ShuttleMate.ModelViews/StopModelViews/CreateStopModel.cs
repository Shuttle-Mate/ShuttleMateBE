using ShuttleMate.Contract.Repositories.Entities;

namespace ShuttleMate.ModelViews.StopModelViews
{
    public class CreateStopModel
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string RefId { get; set; }
        public string WardName { get; set; }
    }
}

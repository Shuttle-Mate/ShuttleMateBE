using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ShuttleLocationRecord : BaseEntity
    {
        public decimal Lat {  get; set; }
        public decimal Lng { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal Accuracy { get; set; }
        public Guid TripId { get; set; }
        public virtual Trip Trip { get; set; }
    }
}

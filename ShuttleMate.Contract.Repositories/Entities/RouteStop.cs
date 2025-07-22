using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class RouteStop : BaseEntity
    {
        public int StopOrder { get; set; }
        public Guid RouteId { get; set; }
        public virtual Route Route { get; set; }
        public Guid StopId { get; set; }
        public virtual Stop Stop { get; set; }
    }
}

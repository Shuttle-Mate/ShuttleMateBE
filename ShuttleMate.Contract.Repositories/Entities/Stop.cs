using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Stop : BaseEntity
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public double Lat {  get; set; }
        public double Lng { get; set; }
        public string RefId { get; set; }
        public Guid WardId { get; set; }
        public virtual Ward Ward { get; set; }
        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public virtual ICollection<StopEstimate> StopEstimates { get; set; } = new List<StopEstimate>();
        public virtual ICollection<Attendance> AttendanceCheckInLocations { get; set; } = new List<Attendance>();
        public virtual ICollection<Attendance> AttendanceCheckOutLocations { get; set; } = new List<Attendance>();

    }
}

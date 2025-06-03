using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Route : BaseEntity
    {
        public string RouteCode { get; set; }
        public string RouteName { get; set; }
        public string OperatingTime { get; set; }
        public decimal Price { get; set; }
        public string OutBound { get; set; }
        public string InBound { get; set; }
        public decimal TotalDistance { get; set; }
        public string RunningTime { get; set; }
        public int AmountOfTrip { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<Stop> Stops { get; set; } = new List<Stop>();
        public virtual ICollection<DepartureTime> DepartureTimes { get; set; } = new List<DepartureTime>();
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();

    }
}

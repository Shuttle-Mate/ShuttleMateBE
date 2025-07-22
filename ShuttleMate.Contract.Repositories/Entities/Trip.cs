using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Trip : BaseEntity
    {
        public Guid RouteId { get; set; }
        public Guid ShuttleId { get; set; }
        public TripDirectionEnum TripDirection { get; set; }
        public DateOnly? TripDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; } = null; // EndTime can be null if the trip hasn't ended yet
        public TripStatusEnum Status { get; set; }
        public virtual Route Route { get; set; }
        public virtual Shuttle Shuttle { get; set; }
        public virtual ICollection<ShuttleLocationRecord> ShuttleLocationRecords { get; set; } = new List<ShuttleLocationRecord>();

    }
}

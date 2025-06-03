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
        public string Name { get; set; }
        public string Ward {  get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<Stop> Stops { get; set; } = new List<Stop>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<ShuttleLocationRecord> ShuttleLocationRecords { get; set; } = new List<ShuttleLocationRecord>();
    }
}

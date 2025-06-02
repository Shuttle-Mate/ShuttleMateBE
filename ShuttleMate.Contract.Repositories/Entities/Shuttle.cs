using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Shuttle : BaseEntity
    {
        public string LicensePlate {  get; set; }
        public Guid OperatorId { get; set; }
        //public enum status { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<ShuttleLocationRecord> ShuttleLocationRecords { get; set; } = new List<ShuttleLocationRecord>();

        public Shuttle()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}

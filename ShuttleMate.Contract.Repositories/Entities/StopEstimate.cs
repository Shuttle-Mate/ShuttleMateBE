using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class StopEstimate : BaseEntity
    {
        public Guid StopId { get; set; }
        public Guid DepartureTimeId { get; set; }
        public TimeOnly ExpectedTime {  get; set; }
        public virtual Stop Stop { get; set; }
        public virtual DepartureTime DepartureTime { get; set; }

    }
}

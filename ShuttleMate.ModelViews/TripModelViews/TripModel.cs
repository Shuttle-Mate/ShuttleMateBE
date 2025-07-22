using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class TripModel
    {
        public DateOnly TripDate { get; set; }
        public TimeOnly StartTime { get; set; }
        //public TimeOnly? EndTime { get; set; }
        //public TripStatusEnum Status { get; set; }
        public Guid ScheduleId { get; set; }
    }
}

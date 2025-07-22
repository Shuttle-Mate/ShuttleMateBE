using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class ResponseTripModel
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public Guid ShuttleId { get; set; }
        //public TripDirectionEnum TripDirection { get; set; }
        public DateOnly TripDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public TripStatusEnum Status { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.ShuttleModelViews
{
    public class ResponseShuttleModel
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; }
        public Guid OperatorId { get; set; }
    }
}

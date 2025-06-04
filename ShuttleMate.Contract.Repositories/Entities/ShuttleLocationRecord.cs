using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ShuttleLocationRecord : BaseEntity
    {
        public Guid TripId { get; set; }
        public decimal Lat {  get; set; }
        public decimal Lng { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal Accuracy { get; set; }
        public virtual Trip Trip { get; set; }
    }
}

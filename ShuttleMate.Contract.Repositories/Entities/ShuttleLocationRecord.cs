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
        public string ShuttleId { get; set; }
        public string RouteId { get; set; }
        public decimal Lat {  get; set; }
        public decimal Lng { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal Accuracy { get; set; }
        public virtual Shuttle Shuttle { get; set; }
        public virtual Route Route { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Attendance : BaseEntity
    {
        public DateTime CheckInTime { get; set; }
        public string CheckInLocation { get; set; }
        public DateTime CheckOutTime { get; set; }
        public string CheckOutLocation { get; set; }
        //public enum status { get; set; }
        public string Notes { get; set; }
    }
}

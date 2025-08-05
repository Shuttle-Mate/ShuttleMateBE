using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.SchoolShiftModelViews
{
    public class SchoolShiftResponse
    {
        public Guid Id { get; set; }
        public TimeOnly Time { get; set; }
        public string ShiftType { get; set; }
        public string SessionType { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class ResponseNumberOfStudentInRoute
    {
        public string DayOfWeek { get; set; }
        public List<NumberOfStudentInRoute> SchoolShift { get; set; }
    }
}

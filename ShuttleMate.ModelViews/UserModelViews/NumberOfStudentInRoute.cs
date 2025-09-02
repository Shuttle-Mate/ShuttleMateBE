using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class NumberOfStudentInRoute
    {
        public Guid SchoolShiftId { get; set; }
        public TimeOnly Time { get; set; }
        public ShiftTypeEnum ShiftType { get; set; }
        public SessionTypeEnum SessionType { get; set; }
        public int NumberOfStudent {  get; set; }
    }
}

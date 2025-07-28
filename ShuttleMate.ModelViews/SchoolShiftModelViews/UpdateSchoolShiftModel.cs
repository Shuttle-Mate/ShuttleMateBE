using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.SchoolShiftModelViews
{
    public class UpdateSchoolShiftModel
    {
        public Guid Id { get; set; }    
        public TimeOnly Time { get; set; }
        public string ShiftType { get; set; }
        public string SessionType { get; set; }
        //public Guid SchoolId { get; set; }
    }
}

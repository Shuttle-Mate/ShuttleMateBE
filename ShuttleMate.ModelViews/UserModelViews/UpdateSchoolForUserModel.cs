using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class UpdateSchoolForUserModel
    {
        public Guid SchoolId { get; set; }
        public List<Guid>? SchoolShifts { get; set; }
    }
}

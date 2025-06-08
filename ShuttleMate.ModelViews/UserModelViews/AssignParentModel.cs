using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class AssignParentModel
    {
        public Guid ParentId { get; set; }
        public Guid UserId { get; set; }
    }
}

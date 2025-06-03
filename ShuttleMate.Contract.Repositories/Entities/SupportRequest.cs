using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class SupportRequest : BaseEntity 
    {
        public SupportRequestCategoryEnum Category { get; set; }
        public SupportRequestStatusEnum Status { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
        public virtual User User { get; set; }
    }
}

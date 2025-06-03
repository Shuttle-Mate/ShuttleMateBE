using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class SupportRequest : BaseEntity 
    {
        //public enum Category { get; set; }
        //public enum Status { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
        public virtual User User { get; set; }
    }
}

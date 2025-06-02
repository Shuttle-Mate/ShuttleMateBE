using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ResponseSupport : BaseEntity
    {
        public string SupportRequestId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public virtual SupportRequest SupportRequest { get; set; }
        //public enum Status { get; set; }
        public ResponseSupport()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}

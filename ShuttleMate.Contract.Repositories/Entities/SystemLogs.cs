using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class SystemLogs : BaseEntity
    {
        public Guid ActorId { get; set; }
        public string Action { get; set; }
        public virtual User Actor { get; set; }
    }
}

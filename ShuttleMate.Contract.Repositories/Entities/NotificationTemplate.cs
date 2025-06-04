using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class NotificationTemplate : BaseEntity
    {
        public string Type { get; set; }
        public string Template {  get; set; }
    }
}

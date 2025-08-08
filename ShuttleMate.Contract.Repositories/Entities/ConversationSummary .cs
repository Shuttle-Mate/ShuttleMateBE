using ShuttleMate.Contract.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ConversationSummary : BaseEntity
    {
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
        public string SummaryContent { get; set; } // Nội dung tóm tắt
    }
}

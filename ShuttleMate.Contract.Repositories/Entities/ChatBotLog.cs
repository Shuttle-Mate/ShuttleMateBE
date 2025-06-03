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
    public class ChatBotLog : BaseEntity
    {
        public ChatBotRoleEnum Role { get; set; }
        public string Content { get; set; }
        public string ModelUsed { get; set; }
        public string UserId { get; set; }
        public virtual User User { get; set; }
    }
}

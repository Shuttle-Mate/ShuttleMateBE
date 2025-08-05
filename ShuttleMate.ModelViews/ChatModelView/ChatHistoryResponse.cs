using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.ChatModelView
{
    public class ChatHistoryResponse
    {
        public Guid Id { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public string ModelUsed { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}

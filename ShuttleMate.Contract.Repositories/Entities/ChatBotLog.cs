using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ChatBotLog : BaseEntity
    {
        public ChatBotRoleEnum Role { get; set; }
        public string Content { get; set; }
        public string ModelUsed { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
    }
}

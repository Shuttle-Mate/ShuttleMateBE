using Microsoft.AspNetCore.Identity;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class UserRole : BaseEntity
    {

        public virtual User User { get; set; }
        public virtual Role Role { get; set; }
        public string UserId { get; set; }
        public string RoleId { get; set; }

        public UserRole()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}

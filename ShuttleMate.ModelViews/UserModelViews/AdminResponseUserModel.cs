using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class AdminResponseUserModel
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; } = string.Empty;
        public bool? Gender { get; set; } = true;
        public DateTime? DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? EmailVerified { get; set; }
        public bool? Violate { get; set; } = false;
        public string? RoleName { get; set; }
        public string? SchoolName { get; set; }
        public string? ParentName { get; set; }
        public DateTimeOffset? DeletedTime { get; set; }
    }
}

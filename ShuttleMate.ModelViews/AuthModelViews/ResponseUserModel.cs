using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.AuthModelViews
{
    public class ResponseUserModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Role { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}

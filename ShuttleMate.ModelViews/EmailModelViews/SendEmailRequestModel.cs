using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.EmailModelViews
{
    public class SendEmailRequestModel
    {
        public List<string>? UserIds { get; set; }
        public List<string>? RoleIds { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}

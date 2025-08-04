using ShuttleMate.ModelViews.SchoolShiftModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.SchoolModelView
{
    public class SchoolResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // Tên trường
        public string? Address { get; set; } // Địa chỉ trường
        public string? PhoneNumber { get; set; } // Số điện thoại liên hệ
        public string? Email { get; set; } // Email liên hệ
        public List<SchoolShiftResponse> schoolShiftResponses { get; set; }
    }
}

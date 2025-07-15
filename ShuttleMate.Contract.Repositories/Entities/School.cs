using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class School : BaseEntity
    {
        public string Name { get; set; } = string.Empty; // Tên trường
        public string? Address { get; set; } // Địa chỉ trường
        public string? PhoneNumber { get; set; } // Số điện thoại liên hệ
        public string? Email { get; set; } // Email liên hệ
        public DateOnly? StartSemOne { get; set; }
        public DateOnly? EndSemOne { get; set; } // Ngày bắt đầu và kết thúc học kỳ 1
        public DateOnly? StartSemTwo { get; set; }
        public DateOnly? EndSemTwo { get; set; } // Ngày bắt đầu và kết thúc học kỳ 2
        public DateTime? SchoolTime { get; set; } // Thời gian vào học của trường
        public bool IsActive { get; set; } = true; // Trạng thái hoạt động của trường
        public virtual ICollection<User> Students { get; set; } = new List<User>(); // Danh sách học sinh thuộc trường
        public virtual ICollection<Route> Routes { get; set; } = new List<Route>(); // Danh sách tuyến đường của trường
    }
}

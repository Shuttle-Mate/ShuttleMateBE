using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.SchoolModelView
{
    public class UpdateSchoolModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // Tên trường
        public string? Address { get; set; } // Địa chỉ trường
        public string? PhoneNumber { get; set; } // Số điện thoại liên hệ
        public string? Email { get; set; } // Email liên hệ
        public DateOnly? StartSemOne { get; set; }
        public DateOnly? EndSemOne { get; set; } // Ngày bắt đầu và kết thúc học kỳ 1
        public DateOnly? StartSemTwo { get; set; }
        public DateOnly? EndSemTwo { get; set; } // Ngày bắt đầu và kết thúc học kỳ 2
        public DateTime? SchoolTime { get; set; } // Thời gian vào học của trường
    }
}

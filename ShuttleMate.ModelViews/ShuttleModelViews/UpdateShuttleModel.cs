using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.ShuttleModelViews
{
    public class UpdateShuttleModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // tên xe
        public string LicensePlate { get; set; }
        public string VehicleType { get; set; } = string.Empty; //loại phương tiện
        public string Color { get; set; } = string.Empty; // màu xe
        public int SeatCount { get; set; } // số ghế
        public string Brand { get; set; } = string.Empty; // hãng xe
        public string Model { get; set; } = string.Empty; // kiểu xe
        public DateTime RegistrationDate { get; set; } // ngày đăng ký
        public DateTime InspectionDate { get; set; } // ngày đăng kiểm gần nhất
        public DateTime NextInspectionDate { get; set; } // ngày đăng kiểm tiếp theo
        public DateTime InsuranceExpiryDate { get; set; } // ngày hết hạn bảo hểm
        public bool IsActive { get; set; } = true; // trạng thái hoạt động của xe
        public bool IsAvailable { get; set; } = true; // trạng thái sẵn sàng của xe
    }
}

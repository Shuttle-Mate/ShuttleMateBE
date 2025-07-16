using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Shuttle : BaseEntity
    {
        public string Name { get; set; } = string.Empty; // tên xe
        public string LicensePlate {  get; set; }
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
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
        public virtual ICollection<DepartureTime> DepartureTimes { get; set; } = new List<DepartureTime>();
        public virtual ICollection<ScheduleOverride> ScheduleOverrides { get; set; } = new List<ScheduleOverride>();
    }
}

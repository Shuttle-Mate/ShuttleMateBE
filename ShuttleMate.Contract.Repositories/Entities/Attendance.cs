using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Attendance : BaseEntity
    {
        public DateTime CheckInTime { get; set; }
        public string? CheckInLocation { get; set; }
        public DateTime CheckOutTime { get; set; }
        public string? CheckOutLocation { get; set; }
        public AttendanceStatusEnum Status { get; set; }
        public string? Notes { get; set; }
        public Guid HistoryTicketId { get; set; }
        public virtual HistoryTicket HistoryTicket { get; set; }
        public Guid TripId { get; set; }
        public virtual Trip Trip { get; set; }
    }
}

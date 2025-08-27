using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Entities;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.AttendanceModelViews
{
    public class ResponseAttendanceModel
    {
        public Guid Id { get; set; }
        public Guid HistoryTicketId { get; set; }
        public Guid TripId { get; set; }
        public DateTime CheckInTime { get; set; }
        public Guid? CheckInLocation { get; set; }
        public DateTime CheckOutTime { get; set; }
        public Guid? CheckOutLocation { get; set; }
        public AttendanceStatusEnum Status { get; set; } 
        public string? Notes { get; set; }
        public TimeOnly? Time { get; set; }
        public ShiftTypeEnum ShiftType { get; set; }
        public SessionTypeEnum SessionType { get; set; }
        //public virtual HistoryTicket HistoryTicket { get; set; }
    }
}

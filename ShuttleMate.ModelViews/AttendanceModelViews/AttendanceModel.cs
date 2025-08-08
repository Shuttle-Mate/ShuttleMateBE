using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Entities;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.AttendanceModelViews
{
    public class AttendanceModel
    {
        public Guid TicketId { get; set; }
        public DateTime CheckInTime { get; set; }
        public Guid? CheckInLocation { get; set; }
        public DateTime CheckOutTime { get; set; }
        public Guid? CheckOutLocation { get; set; }
        public string? Notes { get; set; }
        //public virtual HistoryTicket HistoryTicket { get; set; }
    }

    public class CheckInModel
    {
        public Guid HistoryTicketId { get; set; }
        public Guid TripId { get; set; }
        public Guid? CheckInLocation { get; set; }
        public string? Notes { get; set; }
    }
    public class CheckOutModel
    {
        public Guid Id { get; set; }
        public Guid HistoryTicketId { get; set; }
        public Guid TripId { get; set; }
        public Guid? CheckOutLocation { get; set; }
        public string? Notes { get; set; }
    }
}

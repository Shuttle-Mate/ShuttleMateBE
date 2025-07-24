using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.HistoryTicketModelView
{
    public class CreateHistoryTicketModel
    {
        public Guid TicketId { get; set; }
        public DateOnly ValidFrom { get; set; }
        public List<Guid> ListSchoolShiftId { get; set; }
        //public DateOnly ValidUntil { get; set; }
    }
}

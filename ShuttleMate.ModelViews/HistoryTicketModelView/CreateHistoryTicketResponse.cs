using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.HistoryTicketModelView
{
    public class CreateHistoryTicketResponse
    {
        public Guid HistoryTicketId { get; set; }
        public string checkoutUrl { get; set; }
        public string qrCode { get; set; }
        public string status { get; set; }
    }
}

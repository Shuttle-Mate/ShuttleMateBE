using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.HistoryTicketModelView
{
    public class PayOSResponse
    {
        public string code { get; set; }
        public string desc { get; set; }
        public PayOSResponseData data { get; set; } // Thêm thuộc tính data
        public string signature { get; set; }
    }
}

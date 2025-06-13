using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.HistoryTicketModelView
{
    public class PayOSPaymentRequest
    {
        public int orderCode { get; set; }
        public long amount { get; set; } // Lưu ý: amount cần là kiểu long
        public string description { get; set; }
        public string buyerName { get; set; }
        public string buyerEmail { get; set; }
        public string buyerPhone { get; set; }
        public string buyerAddress { get; set; }
        //public List<PayOSItem> items { get; set; } // Class PayOSItem sẽ được định nghĩa bên dưới
        public string cancelUrl { get; set; }
        public string returnUrl { get; set; }
        public long expiredAt { get; set; } // Unix timestamp
        public string? signature { get; set; }
    }
}

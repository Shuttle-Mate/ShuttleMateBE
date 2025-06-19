using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.HistoryTicketModelView
{
    public class PayOSWebhookRequest
    {
        public string code { get; set; }  // Mã lỗi chung
        public string desc { get; set; }  // Mô tả lỗi
        public bool success { get; set; } // Trạng thái thành công
        public PayOSWebhookData data { get; set; } // Dữ liệu giao dịch
        public string signature { get; set; }   // Chữ ký xác thực
    }

    public class PayOSWebhookData
    {
        public int orderCode { get; set; }
        public long amount { get; set; }
        public string description { get; set; }
        public string accountNumber { get; set; }
        public string reference { get; set; }
        public string transactionDateTime { get; set; }
        public string currency { get; set; }
        public string paymentLinkId { get; set; }
        public string code { get; set; }  // Trạng thái giao dịch (00 = Thành công)
        public string desc { get; set; }
    }
}

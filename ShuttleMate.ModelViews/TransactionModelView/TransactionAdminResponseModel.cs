using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.TransactionModelView
{
    public class TransactionAdminResponseModel
    {
        public Guid Id { get; set; }
        public int? OrderCode { get; set; }
        public string? Description { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
        public string? BuyerAddress { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public string? Status { get; set; }
        public decimal? Amount { get; set; }
        public Guid? HistoryTicketId { get; set; }
    }
}

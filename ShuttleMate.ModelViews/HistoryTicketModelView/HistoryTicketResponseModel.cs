using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.HistoryTicketModelView
{
    public class HistoryTicketResponseModel
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TicketId { get; set; }
        public string? RouteName { get; set; }
        public decimal? Price { get; set; }
        public string? Ticket { get; set; }
        public int? OrderCode { get; set; }
        public string? BuyerName { get; set; }

        public DateTime? PurchaseAt { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidUntil { get; set; }
        public string? Status { get; set; }
    }
}

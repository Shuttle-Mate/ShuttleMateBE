using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Transaction : BaseEntity
    {
        public int OrderCode { get; set; }
        public string? Description { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
        public string? BuyerAddress { get; set; }
        public string? Signature { get; set; }
        public PaymentMethodEnum PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public Guid? HistoryTicketId { get; set; }  
        public virtual HistoryTicket HistoryTicket { get; set; }
        public virtual WithdrawalRequest WithdrawalRequest { get; set; }
    }
}

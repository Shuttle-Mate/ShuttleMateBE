using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class WithdrawalRequest : BaseEntity
    {
        public string OrderCode { get; set; }
        public WithdrawalRequestStatusEnum Status { get; set; }
        public decimal Amount { get; set; }
        public string BankAccount { get; set; }
        public string BankAccountName { get; set; }
        public string BankName { get; set; }
        public string? RejectReason { get; set; }
        public Guid TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
    }
}

namespace ShuttleMate.ModelViews.WithdrawalRequestModelViews
{
    public class ResponseWithdrawalRequestModel
    {
        public string OrderCode { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string BankAccount { get; set; }
        public string BankAccountName { get; set; }
        public string BankName { get; set; }
        public string RejectReason { get; set; }
        public Guid TransactionId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
    }
}

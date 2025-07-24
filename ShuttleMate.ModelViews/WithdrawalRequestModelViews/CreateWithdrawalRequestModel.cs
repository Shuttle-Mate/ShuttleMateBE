namespace ShuttleMate.ModelViews.WithdrawalRequestModelViews
{
    public class CreateWithdrawalRequestModel
    {
        public Guid TransactionId { get; set; }
        public string OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string BankAccount { get; set; }
        public string BankAccountName { get; set; }
        public string BankName { get; set; }
    }
}

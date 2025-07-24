namespace ShuttleMate.ModelViews.WithdrawalRequestModelViews
{
    public class CreateWithdrawalRequestModel
    {
        public Guid TransactionId { get; set; }
        public string BankAccount { get; set; }
        public string BankAccountName { get; set; }
        public string BankName { get; set; }
    }
}

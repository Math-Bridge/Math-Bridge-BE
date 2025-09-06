namespace MathBridge.Domain.Entities
{
    public class WalletTransaction
    {
        public Guid TransactionId { get; set; }
        public Guid ParentId { get; set; }
        public Guid? ContractId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentGatewayReference { get; set; }
        public User Parent { get; set; }
    }
}
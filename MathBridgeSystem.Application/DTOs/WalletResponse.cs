namespace MathBridge.Application.DTOs
{
    public class WalletResponse
    {
        public decimal WalletBalance { get; set; }
        public List<WalletTransactionDto> Transactions { get; set; }

        public class WalletTransactionDto
        {
            public Guid TransactionId { get; set; }
            public decimal Amount { get; set; }
            public string TransactionType { get; set; }
            public string Description { get; set; }
            public DateTime TransactionDate { get; set; }
            public string Status { get; set; }
        }
    }
}
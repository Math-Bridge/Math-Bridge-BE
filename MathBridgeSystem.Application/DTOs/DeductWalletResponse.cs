using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class DeductWalletResponse
    {
        public Guid TransactionId { get; set; }
        public decimal AmountDeducted { get; set; }
        public decimal NewWalletBalance { get; set; }
        public string TransactionStatus { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string Message { get; set; } = null!;
    }
}

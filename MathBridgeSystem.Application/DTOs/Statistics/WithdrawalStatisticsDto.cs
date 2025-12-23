using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.Statistics
{
    public class WithdrawalTransactionDto
    {
        public Guid TransactionId { get; set; }
        public Guid ParentId { get; set; }
        public string? ParentName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
    }

    public class WithdrawalStatisticsDto
    {
        public decimal TotalWithdrawalAmount { get; set; }
        public int TotalWithdrawalCount { get; set; }
        public int PendingWithdrawalCount { get; set; }
        public int CompletedWithdrawalCount { get; set; }
        public int RejectedWithdrawalCount { get; set; }
        public List<WithdrawalTransactionDto> Transactions { get; set; } = new List<WithdrawalTransactionDto>();
    }
}

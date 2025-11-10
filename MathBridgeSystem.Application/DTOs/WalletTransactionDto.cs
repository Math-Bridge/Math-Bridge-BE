using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.WalletTransaction
{
    public class WalletTransactionDto
    {
        public Guid TransactionId { get; set; }
        public Guid ParentId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public Guid? ContractId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? PaymentGatewayReference { get; set; }
        public string? PaymentGateway { get; set; }
    }

    public class CreateWalletTransactionRequest
    {
        [Required]
        public Guid ParentId { get; set; }

        public Guid? ContractId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(200)]
        public string? PaymentGatewayReference { get; set; }

        [MaxLength(50)]
        public string? PaymentGateway { get; set; }
    }

    public class UpdateTransactionStatusRequest
    {
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;
    }
}
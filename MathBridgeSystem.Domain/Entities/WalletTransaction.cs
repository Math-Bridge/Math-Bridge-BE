using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class WalletTransaction
{
    public Guid TransactionId { get; set; }

    public Guid ParentId { get; set; }

    public Guid? ContractId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime TransactionDate { get; set; }

    public string Status { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public string? PaymentGatewayReference { get; set; }

    public string? PaymentGateway { get; set; }

    public virtual Contract? Contract { get; set; }

    public virtual User Parent { get; set; } = null!;

    public virtual ICollection<PayosTransaction> PayosTransactions { get; set; } = new List<PayosTransaction>();

    public virtual ICollection<SepayTransaction> SepayTransactions { get; set; } = new List<SepayTransaction>();
}

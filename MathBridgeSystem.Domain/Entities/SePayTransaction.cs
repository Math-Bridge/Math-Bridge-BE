using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

public partial class SePayTransaction
{
    public Guid SepayTransactionId { get; set; }

    public Guid WalletTransactionId { get; set; }

    public string Gateway { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string? SubAccount { get; set; }

    public string TransferType { get; set; } = null!;

    public decimal TransferAmount { get; set; }

    public decimal Accumulated { get; set; }

    public string Code { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string ReferenceNumber { get; set; } = null!;

    public string? Description { get; set; }

    public string? OrderReference { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual WalletTransaction WalletTransaction { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class PayOstransaction
{
    public Guid PayosTransactionId { get; set; }

    public Guid WalletTransactionId { get; set; }

    public long OrderCode { get; set; }

    public string? PaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public string? ReturnUrl { get; set; }

    public string? CancelUrl { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual WalletTransaction WalletTransaction { get; set; } = null!;
}

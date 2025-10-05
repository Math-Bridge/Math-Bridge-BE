using System;

namespace MathBridge.Domain.Entities;

/// <summary>
/// Represents a PayOS payment transaction
/// </summary>
public partial class PayOSTransaction
{
    /// <summary>
    /// Unique identifier for the PayOS transaction
    /// </summary>
    public Guid PayosTransactionId { get; set; }

    /// <summary>
    /// Reference to the wallet transaction
    /// </summary>
    public Guid WalletTransactionId { get; set; }

    /// <summary>
    /// PayOS order code (unique for each payment)
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>
    /// Payment link ID from PayOS
    /// </summary>
    public string? PaymentLinkId { get; set; }

    /// <summary>
    /// Checkout URL for payment
    /// </summary>
    public string? CheckoutUrl { get; set; }

    /// <summary>
    /// Payment status (PENDING, PAID, CANCELLED)
    /// </summary>
    public string PaymentStatus { get; set; } = null!;

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Return URL after successful payment
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Cancel URL if payment is cancelled
    /// </summary>
    public string? CancelUrl { get; set; }

    /// <summary>
    /// Transaction creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Payment completion date
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Navigation property to wallet transaction
    /// </summary>
    public virtual WalletTransaction WalletTransaction { get; set; } = null!;
}
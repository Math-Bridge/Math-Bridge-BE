using System.ComponentModel.DataAnnotations;

namespace MathBridge.Domain.Entities;

public class SePayTransaction
{
    [Key]
    public Guid SePayTransactionId { get; set; }
    
    /// <summary>
    /// Reference to the wallet transaction
    /// </summary>
    public Guid WalletTransactionId { get; set; }
    
    /// <summary>
    /// SePay gateway identifier
    /// </summary>
    public string Gateway { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction date from SePay
    /// </summary>
    public DateTime TransactionDate { get; set; }
    
    /// <summary>
    /// Bank account number
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Sub account if applicable
    /// </summary>
    public string? SubAccount { get; set; }
    
    /// <summary>
    /// Transfer type: "in" or "out"
    /// </summary>
    public string TransferType { get; set; } = string.Empty;
    
    /// <summary>
    /// Transfer amount from SePay
    /// </summary>
    public decimal TransferAmount { get; set; }
    
    /// <summary>
    /// Accumulated balance
    /// </summary>
    public decimal Accumulated { get; set; }
    
    /// <summary>
    /// SePay transaction code
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction content (contains order reference)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// SePay reference number
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Order reference extracted from content
    /// </summary>
    public string? OrderReference { get; set; }
    
    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property to wallet transaction
    /// </summary>
    public virtual WalletTransaction? WalletTransaction { get; set; }
}
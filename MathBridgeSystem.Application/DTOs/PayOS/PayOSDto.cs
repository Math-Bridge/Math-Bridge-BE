using System.Text.Json.Serialization;

namespace MathBridge.Application.DTOs.PayOS;

/// <summary>
/// Request DTO for creating a PayOS payment link
/// </summary>
public class CreatePayOSPaymentRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

/// <summary>
/// Response DTO for PayOS payment link creation
/// </summary>
public class PayOSPaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string? PaymentLinkId { get; set; }
    public Guid WalletTransactionId { get; set; }
    public Guid PayosTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// PayOS webhook request DTO
/// Maps to PayOS webhook payload structure
/// Based on PayOS documentation
/// </summary>
public class PayOSWebhookRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public PayOSWebhookData? Data { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// PayOS webhook data payload
/// </summary>
public class PayOSWebhookData
{
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("transactionDateTime")]
    public string TransactionDateTime { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";

    [JsonPropertyName("paymentLinkId")]
    public string PaymentLinkId { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("counterAccountBankId")]
    public string? CounterAccountBankId { get; set; }

    [JsonPropertyName("counterAccountBankName")]
    public string? CounterAccountBankName { get; set; }

    [JsonPropertyName("counterAccountName")]
    public string? CounterAccountName { get; set; }

    [JsonPropertyName("counterAccountNumber")]
    public string? CounterAccountNumber { get; set; }

    [JsonPropertyName("virtualAccountName")]
    public string? VirtualAccountName { get; set; }

    [JsonPropertyName("virtualAccountNumber")]
    public string? VirtualAccountNumber { get; set; }
}

/// <summary>
/// PayOS webhook processing result
/// </summary>
public class PayOSWebhookResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? WalletTransactionId { get; set; }
    public long? OrderCode { get; set; }
    public string? PaymentStatus { get; set; }
}

/// <summary>
/// Request to check PayOS payment status
/// </summary>
public class CheckPayOSPaymentRequest
{
    public long OrderCode { get; set; }
}

/// <summary>
/// PayOS payment status response
/// </summary>
public class PayOSPaymentStatusResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // PENDING, PAID, CANCELLED
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentLinkId { get; set; }
    public Guid? WalletTransactionId { get; set; }
}

/// <summary>
/// Request to cancel a PayOS payment
/// </summary>
public class CancelPayOSPaymentRequest
{
    public long OrderCode { get; set; }
    public string? CancellationReason { get; set; }
}

/// <summary>
/// Response for PayOS payment cancellation
/// </summary>
public class CancelPayOSPaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request to get user's PayOS transaction history
/// </summary>
public class GetPayOSTransactionsRequest
{
    public Guid UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// PayOS transaction item for listing
/// </summary>
public class PayOSTransactionDto
{
    public Guid PayosTransactionId { get; set; }
    public Guid WalletTransactionId { get; set; }
    public long OrderCode { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? PaidAt { get; set; }
}

/// <summary>
/// Response containing list of PayOS transactions
/// </summary>
public class PayOSTransactionsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<PayOSTransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
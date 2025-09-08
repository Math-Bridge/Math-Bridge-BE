using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;

namespace MathBridge.Application.DTOs.SePay;

/// <summary>
/// Custom DateTime converter for SePay date format: "yyyy-MM-dd HH:mm:ss"
/// </summary>
public class CustomDateTimeConverter : JsonConverter<DateTime>
{
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }
        
        // Fallback to default parsing
        return DateTime.Parse(dateString ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat));
    }
}

/// <summary>
/// SePay webhook payload structure
/// Based on SePay documentation: https://docs.sepay.vn/tich-hop-webhooks.html#du-lieu
/// </summary>
public class SePayWebhookRequestDto
{
    [JsonPropertyName("gateway")]
    public string Gateway { get; set; } = string.Empty;

    [JsonPropertyName("transactionDate")]
    [JsonConverter(typeof(CustomDateTimeConverter))]
    public DateTime TransactionDate { get; set; }

    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("subAccount")]
    public string? SubAccount { get; set; }

    [JsonPropertyName("transferType")]
    public string TransferType { get; set; } = string.Empty;

    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; set; }

    [JsonPropertyName("accumulated")]
    public decimal Accumulated { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("referenceCode")]
    public string ReferenceCode { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// SePay payment request for creating payment QR codes
/// </summary>
public class SePayPaymentRequestDto
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string BankCode { get; set; } =string.Empty;
}

/// <summary>
/// SePay payment response with QR code information
/// </summary>
public class SePayPaymentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string OrderReference { get; set; } = string.Empty;
    public Guid WalletTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string BankInfo { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
}

/// <summary>
/// Payment status check response
/// </summary>
public class PaymentStatusDto
{
    public string Status { get; set; } = string.Empty; // "Paid", "Unpaid", "Pending"
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal? AmountPaid { get; set; }
}

/// <summary>
/// SePay webhook processing result
/// </summary>
public class SePayWebhookResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? WalletTransactionId { get; set; }
    public string? OrderReference { get; set; }
}
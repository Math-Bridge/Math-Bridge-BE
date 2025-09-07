using MathBridge.Application.DTOs.SePay;

namespace MathBridge.Application.Interfaces
{


    /// <summary>
    /// Service interface for SePay payment gateway operations
    /// </summary>
    public interface ISePayService
    {
        /// <summary>
        /// Create a payment request and generate QR code for bank transfer
        /// </summary>
        /// <param name="request">Payment request details</param>
        /// <returns>Payment response with QR code information</returns>
        Task<SePayPaymentResponseDto> CreatePaymentRequestAsync(SePayPaymentRequestDto request);

        /// <summary>
        /// Process incoming webhook from SePay
        /// </summary>
        /// <param name="webhookData">Webhook payload from SePay</param>
        /// <returns>Processing result</returns>
        Task<SePayWebhookResultDto> ProcessWebhookAsync(SePayWebhookDto webhookData);

        /// <summary>
        /// Check payment status for a wallet transaction
        /// </summary>
        /// <param name="walletTransactionId">Wallet transaction ID</param>
        /// <returns>Current payment status</returns>
        Task<PaymentStatusDto> CheckPaymentStatusAsync(Guid walletTransactionId);

        /// <summary>
        /// Get payment details including QR code URL
        /// </summary>
        /// <param name="walletTransactionId">Wallet transaction ID</param>
        /// <returns>Payment details</returns>
        Task<SePayPaymentResponseDto?> GetPaymentDetailsAsync(Guid walletTransactionId);

        /// <summary>
        /// Generate SePay QR code URL for bank transfer
        /// </summary>
        /// <param name="amount">Transfer amount</param>
        /// <param name="description">Transfer description/reference</param>
        /// <param name="bankCode">Bank code (default: MBBank)</param>
        /// <returns>QR code URL</returns>
        string GenerateQrCodeUrl(decimal amount, string description, string bankCode = "MBBank");

        /// <summary>
        /// Extract order reference from SePay transaction content
        /// </summary>
        /// <param name="content">Transaction content from webhook</param>
        /// <returns>Extracted order reference or null if not found</returns>
        string? ExtractOrderReference(string content);

        /// <summary>
        /// Validate webhook signature (if SePay provides signature validation)
        /// </summary>
        /// <param name="payload">Raw webhook payload</param>
        /// <param name="signature">Webhook signature</param>
        /// <returns>True if signature is valid</returns>
        bool ValidateWebhookSignature(string payload, string? signature);
    }
}
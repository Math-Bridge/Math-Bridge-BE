using MathBridge.Application.DTOs.PayOS;

namespace MathBridge.Application.Interfaces;

/// <summary>
/// Service interface for PayOS payment gateway operations
/// Handles payment link creation, webhook processing, and payment status management
/// </summary>
public interface IPayOSService
{
    /// <summary>
    /// Create a payment link using PayOS
    /// Generates a unique order code and creates a payment link
    /// </summary>
    /// <param name="request">Payment request details including amount and user info</param>
    /// <returns>Payment response with checkout URL and order details</returns>
    Task<PayOSPaymentResponse> CreatePaymentLinkAsync(CreatePayOSPaymentRequest request);

    /// <summary>
    /// Process incoming webhook notification from PayOS
    /// Verifies webhook signature and updates payment status
    /// </summary>
    /// <param name="webhookData">Webhook payload from PayOS</param>
    /// <returns>Processing result indicating success or failure</returns>
    Task<PayOSWebhookResult> ProcessWebhookAsync(PayOSWebhookRequest webhookData);

    /// <summary>
    /// Check payment status by order code
    /// Queries PayOS API for current payment status
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <returns>Current payment status information</returns>
    Task<PayOSPaymentStatusResponse> CheckPaymentStatusAsync(long orderCode);

    /// <summary>
    /// Get payment details by wallet transaction ID
    /// Retrieves stored payment information from database
    /// </summary>
    /// <param name="walletTransactionId">Wallet transaction ID</param>
    /// <returns>Payment details or null if not found</returns>
    Task<PayOSPaymentResponse?> GetPaymentDetailsByWalletTransactionAsync(Guid walletTransactionId);

    /// <summary>
    /// Cancel a pending payment
    /// Cancels the payment link in PayOS and updates local status
    /// </summary>
    /// <param name="request">Cancellation request with order code and reason</param>
    /// <returns>Cancellation result</returns>
    Task<CancelPayOSPaymentResponse> CancelPaymentAsync(CancelPayOSPaymentRequest request);

    /// <summary>
    /// Get user's PayOS transaction history with pagination
    /// </summary>
    /// <param name="request">Request containing user ID and pagination parameters</param>
    /// <returns>List of PayOS transactions</returns>
    Task<PayOSTransactionsResponse> GetUserTransactionsAsync(GetPayOSTransactionsRequest request);

    /// <summary>
    /// Generate unique order code for PayOS
    /// Creates a unique long integer based on timestamp
    /// </summary>
    /// <returns>Unique order code</returns>
    long GenerateOrderCode();

    /// <summary>
    /// Verify webhook signature from PayOS
    /// Uses PayOS SDK to validate webhook authenticity
    /// </summary>
    /// <param name="webhookData">Webhook request with signature</param>
    /// <returns>True if signature is valid</returns>
    bool VerifyWebhookSignature(PayOSWebhookRequest webhookData);

    /// <summary>
    /// Sync payment status with PayOS
    /// Fetches latest status from PayOS API and updates local database
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <returns>Updated payment status</returns>
    Task<PayOSPaymentStatusResponse> SyncPaymentStatusAsync(long orderCode);
}
using Net.payOS;
using Net.payOS.Types;
using Microsoft.Extensions.Logging;

namespace MathBridge.Infrastructure.Services;

/// <summary>
/// Gateway service that wraps PayOS SDK functionality
/// Provides payment link creation, retrieval, cancellation, and webhook verification
/// </summary>
public class PayOSGatewayService
{
    private readonly PayOS _payOS;
    private readonly ILogger<PayOSGatewayService> _logger;

    public PayOSGatewayService(PayOS payOS, ILogger<PayOSGatewayService> logger)
    {
        _payOS = payOS;
        _logger = logger;
    }

    /// <summary>
    /// Create payment link using PayOS SDK
    /// </summary>
    /// <param name="paymentData">Payment data containing order information</param>
    /// <returns>Payment creation result with checkout URL</returns>
    /// <exception cref="Exception">Throws when payment link creation fails</exception>
    public async Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData paymentData)
    {
        try
        {
            _logger.LogInformation("Creating PayOS payment link for order code: {OrderCode}", paymentData.orderCode);
            var result = await _payOS.createPaymentLink(paymentData);
            _logger.LogInformation("Successfully created PayOS payment link. Checkout URL: {CheckoutUrl}", result.checkoutUrl);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment link for order code: {OrderCode}", paymentData.orderCode);
            throw;
        }
    }

    /// <summary>
    /// Get payment information from PayOS
    /// </summary>
    /// <param name="orderCode">The order code to retrieve information for</param>
    /// <returns>Payment link information including status and details</returns>
    /// <exception cref="Exception">Throws when payment retrieval fails</exception>
    public async Task<PaymentLinkInformation> GetPaymentInfoAsync(long orderCode)
    {
        try
        {
            _logger.LogInformation("Getting PayOS payment info for order code: {OrderCode}", orderCode);
            var result = await _payOS.getPaymentLinkInformation(orderCode);
            _logger.LogInformation("Retrieved PayOS payment info for order code: {OrderCode}, Status: {Status}", 
                orderCode, result.status);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayOS payment info for order code: {OrderCode}", orderCode);
            throw;
        }
    }

    /// <summary>
    /// Cancel payment link in PayOS
    /// </summary>
    /// <param name="orderCode">The order code to cancel</param>
    /// <param name="cancellationReason">Reason for cancellation (optional)</param>
    /// <returns>Updated payment link information after cancellation</returns>
    /// <exception cref="Exception">Throws when payment cancellation fails</exception>
    public async Task<PaymentLinkInformation> CancelPaymentLinkAsync(long orderCode, string? cancellationReason = null)
    {
        try
        {
            var reason = cancellationReason ?? "User cancelled";
            _logger.LogInformation("Cancelling PayOS payment for order code: {OrderCode}, Reason: {Reason}", 
                orderCode, reason);
            var result = await _payOS.cancelPaymentLink(orderCode, reason);
            _logger.LogInformation("Successfully cancelled PayOS payment for order code: {OrderCode}", orderCode);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling PayOS payment for order code: {OrderCode}", orderCode);
            throw;
        }
    }

    /// <summary>
    /// Verify webhook data from PayOS
    /// Ensures the webhook payload is authentic and hasn't been tampered with
    /// </summary>
    /// <param name="webhookBody">The webhook data received from PayOS</param>
    /// <returns>Verified webhook data</returns>
    /// <exception cref="Exception">Throws when webhook verification fails</exception>
    public WebhookData VerifyWebhookData(WebhookType webhookBody)
    {
        try
        {
            _logger.LogInformation("Verifying PayOS webhook data for order code: {OrderCode}", 
                webhookBody.data?.orderCode ?? 0);
            var verifiedData = _payOS.verifyPaymentWebhookData(webhookBody);
            _logger.LogInformation("Successfully verified PayOS webhook data");
            return verifiedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayOS webhook data");
            throw;
        }
    }

    /// <summary>
    /// Confirm webhook delivery to PayOS
    /// Call this after successfully processing a webhook to acknowledge receipt
    /// </summary>
    /// <param name="webhookUrl">The webhook URL to confirm</param>
    /// <returns>Confirmation result</returns>
    public async Task<string> ConfirmWebhookAsync(string webhookUrl)
    {
        try
        {
            _logger.LogInformation("Confirming webhook URL: {WebhookUrl}", webhookUrl);
            var result = await _payOS.confirmWebhook(webhookUrl);
            _logger.LogInformation("Successfully confirmed webhook");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming webhook URL: {WebhookUrl}", webhookUrl);
            throw;
        }
    }
}
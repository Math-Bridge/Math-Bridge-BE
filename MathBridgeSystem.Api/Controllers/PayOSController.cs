using MathBridge.Application.DTOs.PayOS;
using MathBridge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathBridge.Api.Controllers;

/// <summary>
/// PayOS payment gateway controller
/// Handles payment link creation, webhook processing, and payment management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PayOSController : ControllerBase
{
    private readonly IPayOSService _payOSService;
    private readonly ILogger<PayOSController> _logger;

    public PayOSController(IPayOSService payOSService, ILogger<PayOSController> logger)
    {
        _payOSService = payOSService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment link for wallet deposit
    /// </summary>
    /// <param name="request">Payment request details including amount and description</param>
    /// <returns>Payment response with checkout URL and order details</returns>
    [HttpPost("create-payment")]
    [Authorize]
    public async Task<ActionResult<PayOSPaymentResponse>> CreatePayment([FromBody] CreatePayOSPaymentRequest request)
    {
        try
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            // Override request user ID with authenticated user ID for security
            request.UserId = userId;

            // Validate request
            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than 0" });
            }

            if (request.Amount > 50000000) // 50M VND limit
            {
                return BadRequest(new { message = "Amount exceeds maximum limit of 50,000,000 VND" });
            }

            if (request.Amount < 2000) // Minimum 2,000 VND for PayOS
            {
                return BadRequest(new { message = "Amount must be at least 2,000 VND" });
            }

            var result = await _payOSService.CreatePaymentLinkAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Payment link created successfully for user {UserId}, order code {OrderCode}",
                userId, result.OrderCode);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment link");
            return StatusCode(500, new { message = "An error occurred while creating payment link" });
        }
    }

    /// <summary>
    /// PayOS webhook endpoint for receiving payment notifications
    /// This endpoint is called by PayOS when payment status changes
    /// </summary>
    /// <param name="webhookData">Webhook payload from PayOS</param>
    /// <returns>Processing result</returns>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<PayOSWebhookResult>> ReceiveWebhook([FromBody] PayOSWebhookRequest webhookData)
    {
        try
        {
            _logger.LogInformation("Received PayOS webhook for order code {OrderCode}",
                webhookData.Data?.OrderCode ?? 0);

            var result = await _payOSService.ProcessWebhookAsync(webhookData);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to process webhook: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Successfully processed PayOS webhook for order code {OrderCode}",
                webhookData.Data?.OrderCode ?? 0);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
            return StatusCode(500, new { message = "An error occurred while processing webhook" });
        }
    }

    /// <summary>
    /// Check payment status by order code
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <returns>Payment status information</returns>
    [HttpGet("payment-status/{orderCode}")]
    [Authorize]
    public async Task<ActionResult<PayOSPaymentStatusResponse>> CheckPaymentStatus(long orderCode)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var result = await _payOSService.CheckPaymentStatusAsync(orderCode);

            if (!result.Success)
            {
                return NotFound(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status for order code {OrderCode}", orderCode);
            return StatusCode(500, new { message = "An error occurred while checking payment status" });
        }
    }

    /// <summary>
    /// Get payment details by wallet transaction ID
    /// </summary>
    /// <param name="walletTransactionId">Wallet transaction ID</param>
    /// <returns>Payment details with checkout URL</returns>
    [HttpGet("payment-details/{walletTransactionId}")]
    [Authorize]
    public async Task<ActionResult<PayOSPaymentResponse>> GetPaymentDetails(Guid walletTransactionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var result = await _payOSService.GetPaymentDetailsByWalletTransactionAsync(walletTransactionId);

            if (result == null)
            {
                return NotFound(new { message = "Payment details not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment details for wallet transaction {WalletTransactionId}",
                walletTransactionId);
            return StatusCode(500, new { message = "An error occurred while retrieving payment details" });
        }
    }

    /// <summary>
    /// Cancel a pending payment
    /// </summary>
    /// <param name="request">Cancellation request with order code and optional reason</param>
    /// <returns>Cancellation result</returns>
    [HttpPost("cancel-payment")]
    [Authorize]
    public async Task<ActionResult<CancelPayOSPaymentResponse>> CancelPayment([FromBody] CancelPayOSPaymentRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var result = await _payOSService.CancelPaymentAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Payment cancelled successfully for order code {OrderCode}", request.OrderCode);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment for order code {OrderCode}", request.OrderCode);
            return StatusCode(500, new { message = "An error occurred while cancelling payment" });
        }
    }

    /// <summary>
    /// Get user's PayOS transaction history with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>List of user's PayOS transactions</returns>
    [HttpGet("my-transactions")]
    [Authorize]
    public async Task<ActionResult<PayOSTransactionsResponse>> GetMyTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            // Validate pagination parameters
            if (pageNumber < 1)
            {
                return BadRequest(new { message = "Page number must be greater than 0" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Page size must be between 1 and 100" });
            }

            var request = new GetPayOSTransactionsRequest
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _payOSService.GetUserTransactionsAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for user");
            return StatusCode(500, new { message = "An error occurred while retrieving transactions" });
        }
    }

    /// <summary>
    /// Sync payment status with PayOS API
    /// Fetches the latest status from PayOS and updates local database
    /// </summary>
    /// <param name="orderCode">PayOS order code</param>
    /// <returns>Updated payment status</returns>
    [HttpPost("sync-status/{orderCode}")]
    [Authorize]
    public async Task<ActionResult<PayOSPaymentStatusResponse>> SyncPaymentStatus(long orderCode)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var result = await _payOSService.SyncPaymentStatusAsync(orderCode);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Payment status synced for order code {OrderCode}", orderCode);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing payment status for order code {OrderCode}", orderCode);
            return StatusCode(500, new { message = "An error occurred while syncing payment status" });
        }
    }

    /// <summary>
    /// Health check endpoint for PayOS integration
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            service = "PayOS Integration",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            provider = "PayOS",
            features = new[]
            {
                "Payment Link Creation",
                "Webhook Processing",
                "Status Checking",
                "Payment Cancellation",
                "Transaction History"
            }
        });
    }
}
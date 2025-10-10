using MathBridgeSystem.Application.DTOs.SePay;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathBridgeSystem.Api.Controllers;

/// <summary>
/// SePay payment gateway controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SePayController : ControllerBase
{
    private readonly ISePayService _sePayService;
    private readonly ILogger<SePayController> _logger;

    public SePayController(ISePayService sePayService, ILogger<SePayController> logger)
    {
        _sePayService = sePayService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment request with QR code for wallet deposit
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Payment response with QR code information</returns>
    [HttpPost("create-payment")]
    [Authorize]
    public async Task<ActionResult<SePayPaymentResponseDto>> CreatePayment([FromBody] SePayPaymentRequestDto request)
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

            var result = await _sePayService.CreatePaymentRequestAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Payment request created successfully for user {UserId}, transaction {TransactionId}", 
                userId, result.WalletTransactionId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SePay payment request");
            return StatusCode(500, new { message = "An error occurred while creating payment request" });
        }
    }

    /// <summary>
    /// SePay webhook endpoint for receiving payment notifications
    /// </summary>
    /// <param name="webhookData">Webhook payload from SePay</param>
    /// <returns>Processing result</returns>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<SePayWebhookResultDto>> ReceiveWebhook([FromBody] SePayWebhookRequestDto request)
    {
        try
        {
            var webhookData = request;
            _logger.LogInformation("Received SePay webhook for transaction {Code}", webhookData.Code);
            

            var result = await _sePayService.ProcessWebhookAsync(webhookData);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to process webhook: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("Successfully processed SePay webhook for transaction {Code}", webhookData.Code);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SePay webhook");
            return StatusCode(500, new { message = "An error occurred while processing webhook" });
        }
    }

    /// <summary>
    /// Check payment status for a specific transaction
    /// </summary>
    /// <param name="transactionId">Wallet transaction ID</param>
    /// <returns>Payment status</returns>
    [HttpGet("payment-status/{transactionId}")]
    [Authorize]
    public async Task<ActionResult<PaymentStatusDto>> CheckPaymentStatus(Guid transactionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var result = await _sePayService.CheckPaymentStatusAsync(transactionId);

            if (!result.Success)
            {
                return NotFound(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status for transaction {TransactionId}", transactionId);
            return StatusCode(500, new { message = "An error occurred while checking payment status" });
        }
    }

    /// <summary>
    /// Get payment details including QR code for a specific transaction
    /// </summary>
    /// <param name="transactionId">Wallet transaction ID</param>
    /// <returns>Payment details with QR code</returns>
    [HttpGet("payment-details/{transactionId}")]
    [Authorize]
    public async Task<ActionResult<SePayPaymentResponseDto>> GetPaymentDetails(Guid transactionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var result = await _sePayService.GetPaymentDetailsAsync(transactionId);

            if (result == null)
            {
                return NotFound(new { message = "Payment details not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment details for transaction {TransactionId}", transactionId);
            return StatusCode(500, new { message = "An error occurred while retrieving payment details" });
        }
    }

    /// <summary>
    /// Generate QR code URL for manual testing (development only)
    /// </summary>
    /// <param name="amount">Amount in VND</param>
    /// <param name="description">Transfer description</param>
    /// <returns>QR code URL</returns>
    [HttpGet("generate-qr")]
    public ActionResult<object> GenerateQrCode([FromQuery] decimal amount, [FromQuery] string description)
    {
        try
        {
            if (amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than 0" });
            }

            var qrUrl = _sePayService.GenerateQrCodeUrl(amount, description);
            
            return Ok(new { 
                qrCodeUrl = qrUrl,
                amount = amount,
                description = description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return StatusCode(500, new { message = "An error occurred while generating QR code" });
        }
    }

    /// <summary>
    /// Health check endpoint for SePay integration
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            service = "SePay Integration",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
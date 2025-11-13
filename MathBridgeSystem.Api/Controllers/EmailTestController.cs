using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("send-verification")]
        public async Task<IActionResult> SendVerificationEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var verificationLink = "https://mathbridge.com/verify?token=test-token-12345";
                await _emailService.SendVerificationLinkAsync(email, verificationLink);
                
                _logger.LogInformation($"Verification email sent to {email}");
                return Ok(new { message = "Verification email sent successfully", email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification email to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-reset-password")]
        public async Task<IActionResult> SendResetPasswordEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var resetLink = "https://mathbridge.com/reset-password?token=reset-token-12345";
                await _emailService.SendResetPasswordLinkAsync(email, resetLink);
                
                _logger.LogInformation($"Password reset email sent to {email}");
                return Ok(new { message = "Password reset email sent successfully", email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-session-reminder")]
        public async Task<IActionResult> SendSessionReminder([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var studentName = "John Doe";
                var tutorName = "Jane Smith";
                var sessionDateTime = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss");
                var sessionLink = "https://meet.mathbridge.com/session/abc123";

                await _emailService.SendSessionReminderAsync(email, studentName, tutorName, sessionDateTime, sessionLink);
                
                _logger.LogInformation($"Session reminder sent to {email}");
                return Ok(new { message = "Session reminder sent successfully", email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send session reminder to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-invoice")]
        public async Task<IActionResult> SendInvoice([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var studentName = "John Doe";
                var invoiceNumber = "INV-2025-001";
                var amount = "$500.00";
                var dueDate = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
                var invoiceUrl = "https://mathbridge.com/invoices/inv-2025-001";

                await _emailService.SendInvoiceAsync(email, studentName, invoiceNumber, amount, dueDate, invoiceUrl);
                
                _logger.LogInformation($"Invoice email sent to {email}");
                return Ok(new { message = "Invoice email sent successfully", email, invoiceNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send invoice to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-progress-report")]
        public async Task<IActionResult> SendProgressReport([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var studentName = "John Doe";
                var reportPeriod = "November 2025";
                var reportUrl = "https://mathbridge.com/reports/nov-2025";

                await _emailService.SendProgressReportAsync(email, studentName, reportPeriod, reportUrl);
                
                _logger.LogInformation($"Progress report sent to {email}");
                return Ok(new { message = "Progress report sent successfully", email, reportPeriod });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send progress report to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-refund-confirmation")]
        public async Task<IActionResult> SendRefundConfirmation([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var studentName = "John Doe";
                var refundAmount = "$250.00";
                var refundDate = DateTime.Now.ToString("yyyy-MM-dd");

                await _emailService.SendRefundConfirmationAsync(email, studentName, refundAmount, refundDate);
                
                _logger.LogInformation($"Refund confirmation sent to {email}");
                return Ok(new { message = "Refund confirmation sent successfully", email, refundAmount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send refund confirmation to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-contract-cancelled")]
        public async Task<IActionResult> SendContractCancelled([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var studentName = "John Doe";
                var reason = "Student requested cancellation";
                var cancellationDate = DateTime.Now.ToString("yyyy-MM-dd");

                await _emailService.SendContractCancelledAsync(email, studentName, reason, cancellationDate);
                
                _logger.LogInformation($"Contract cancellation notice sent to {email}");
                return Ok(new { message = "Contract cancellation notice sent successfully", email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send contract cancellation notice to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-all")]
        public async Task<IActionResult> SendAllEmails([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest("Email parameter is required");

                var results = new System.Collections.Generic.List<object>();

                try
                {
                    await _emailService.SendVerificationLinkAsync(email, "https://mathbridge.com/verify?token=test");
                    results.Add(new { type = "Verification", status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { type = "Verification", status = "failed", error = ex.Message });
                }

                try
                {
                    await _emailService.SendResetPasswordLinkAsync(email, "https://mathbridge.com/reset?token=test");
                    results.Add(new { type = "ResetPassword", status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { type = "ResetPassword", status = "failed", error = ex.Message });
                }

                try
                {
                    await _emailService.SendSessionReminderAsync(email, "Student", "Tutor", DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm"), "https://meet.test.com");
                    results.Add(new { type = "SessionReminder", status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { type = "SessionReminder", status = "failed", error = ex.Message });
                }

                try
                {
                    await _emailService.SendInvoiceAsync(email, "Student", "INV-001", "500", DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"), "https://mathbridge.com/inv");
                    results.Add(new { type = "Invoice", status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { type = "Invoice", status = "failed", error = ex.Message });
                }

                try
                {
                    await _emailService.SendProgressReportAsync(email, "Student", "Nov 2025", "https://mathbridge.com/report");
                    results.Add(new { type = "ProgressReport", status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { type = "ProgressReport", status = "failed", error = ex.Message });
                }

                try
                {
                    await _emailService.SendRefundConfirmationAsync(email, "Student", "250", DateTime.Now.ToString("yyyy-MM-dd"));
                    results.Add(new { type = "RefundConfirmation", status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { type = "RefundConfirmation", status = "failed", error = ex.Message });
                }

                try
                {
                    await _emailService.SendContractCancelledAsync(email, "Student", "Student request", DateTime.Now.ToString("yyyy-MM-dd"));
                    results.Add(new { type = "ContractCancelled", status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { type = "ContractCancelled", status = "failed", error = ex.Message });
                }

                _logger.LogInformation($"All emails attempted for {email}");
                return Ok(new { message = "All emails processed", email, results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send batch emails to {email}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
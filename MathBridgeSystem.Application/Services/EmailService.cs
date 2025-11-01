using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _serviceAccountEmail;
        private readonly string _serviceAccountPrivateKey;
        private readonly string _adminEmail;
        private readonly string _fromEmail;
        private readonly ILogger<EmailService> _logger;
        private GmailService _gmailService;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _logger = logger;
            _serviceAccountEmail = configuration["Gmail:ServiceAccountEmail"] ?? throw new ArgumentNullException("Gmail:ServiceAccountEmail");
            _serviceAccountPrivateKey = configuration["Gmail:ServiceAccountPrivateKey"] ?? throw new ArgumentNullException("Gmail:ServiceAccountPrivateKey");
            _adminEmail = configuration["Gmail:AdminEmail"] ?? throw new ArgumentNullException("Gmail:AdminEmail");
            _fromEmail = configuration["Gmail:FromEmail"] ?? throw new ArgumentNullException("Gmail:FromEmail");
            
            InitializeGmailService();
        }

        private void InitializeGmailService()
        {
            try
            {
                var credential = CreateCredential();
                _gmailService = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "MathBridge Email Service"
                });
                _logger.LogInformation("Gmail service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Gmail service");
                throw;
            }
        }

        private ServiceAccountCredential CreateCredential()
        {
            var keyStream = new MemoryStream(Encoding.UTF8.GetBytes(_serviceAccountPrivateKey));
            var credential = ServiceAccountCredential.FromServiceAccountData(keyStream);
            var scopedCredential = credential.CreateScoped(
                GmailService.Scope.GmailSend)
                .CreateWithUser(_adminEmail);
            
            return (ServiceAccountCredential)scopedCredential;
        }

        public async Task SendVerificationLinkAsync(string email, string link)
        {
            try
            {
                var subject = "Your MathBridge Verification Code";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #f8f9fa; padding: 10px; text-align: center; border-bottom: 1px solid #e0e0e0; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>MathBridge</h2>
            <p>Welcome to Your Learning Journey!</p>
        </div>
        <div style='padding: 20px; text-align: center;'>
            <p>Dear User,</p>
            <p>Thank you for registering with MathBridgeSystem. To complete your account setup, please click the button below to verify your email:</p>
            <a href='{link}' class='button'>Verify Email</a>
            <p>This link will expire in 5 minutes. If you did not request this, please ignore this email or contact our support team.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
            <p>For assistance, email us at <a href='mailto:{_fromEmail}'>{_fromEmail}</a>.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Verification link sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification link to {email}");
                throw;
            }
        }

        public async Task SendResetPasswordLinkAsync(string email, string link)
        {
            try
            {
                var subject = "Reset Your MathBridge Password";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #f8f9fa; padding: 10px; text-align: center; border-bottom: 1px solid #e0e0e0; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>MathBridge</h2>
            <p>Password Reset</p>
        </div>
        <div style='padding: 20px; text-align: center;'>
            <p>Dear User,</p>
            <p>You requested to reset your password. Click the button below to verify and set a new password:</p>
            <a href='{link}' class='button'>Reset Password</a>
            <p>This link will expire in 15 minutes. If you didn't request this, ignore this email.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
            <p>For assistance, email us at <a href='mailto:{_fromEmail}'>{_fromEmail}</a>.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Password reset link sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset link to {email}");
                throw;
            }
        }

        public async Task SendSessionReminderAsync(string email, string studentName, string tutorName, string sessionDateTime, string sessionLink)
        {
            try
            {
                var subject = "Upcoming Session Reminder - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #007bff; padding: 20px; text-align: center; color: white; }}
        .content {{ padding: 20px; }}
        .session-info {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; margin: 15px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #28a745; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Session Reminder</h2>
        </div>
        <div class='content'>
            <p>Hi {studentName},</p>
            <p>This is a reminder that you have an upcoming tutoring session:</p>
            <div class='session-info'>
                <p><strong>Tutor:</strong> {tutorName}</p>
                <p><strong>Date & Time:</strong> {sessionDateTime}</p>
            </div>
            <p>Click the button below to join your session:</p>
            <a href='{sessionLink}' class='button'>Join Session</a>
            <p>If you need to reschedule, please contact your tutor.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Session reminder sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send session reminder to {email}");
                throw;
            }
        }

        public async Task SendInvoiceAsync(string email, string studentName, string invoiceNumber, string amount, string dueDate, string invoiceUrl)
        {
            try
            {
                var subject = $"Invoice {invoiceNumber} - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-bottom: 2px solid #007bff; }}
        .invoice-info {{ background-color: #f8f9fa; padding: 20px; margin: 20px 0; }}
        .amount {{ font-size: 24px; color: #007bff; font-weight: bold; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Invoice {invoiceNumber}</h2>
        </div>
        <div style='padding: 20px;'>
            <p>Dear {studentName},</p>
            <p>Your invoice is ready. Please find the details below:</p>
            <div class='invoice-info'>
                <p><strong>Invoice Number:</strong> {invoiceNumber}</p>
                <p><strong>Amount Due:</strong> <span class='amount'>{amount}</span></p>
                <p><strong>Due Date:</strong> {dueDate}</p>
            </div>
            <p>Click the button below to view or download your invoice:</p>
            <a href='{invoiceUrl}' class='button'>View Invoice</a>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Invoice {invoiceNumber} sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send invoice to {email}");
                throw;
            }
        }

        public async Task SendProgressReportAsync(string email, string studentName, string reportPeriod, string reportUrl)
        {
            try
            {
                var subject = $"Your Progress Report - {reportPeriod} - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #28a745; padding: 20px; text-align: center; color: white; }}
        .content {{ padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #28a745; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Your Progress Report</h2>
        </div>
        <div class='content'>
            <p>Dear {studentName},</p>
            <p>Your progress report for {reportPeriod} is now available!</p>
            <p>Review your achievements, areas for improvement, and next steps to continue your learning journey with us.</p>
            <a href='{reportUrl}' class='button'>View Report</a>
            <p>If you have any questions about your progress, please reach out to your tutor.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Progress report for period {reportPeriod} sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send progress report to {email}");
                throw;
            }
        }

        public async Task SendRefundConfirmationAsync(string email, string studentName, string refundAmount, string refundDate)
        {
            try
            {
                var subject = "Refund Confirmation - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #ffc107; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .refund-info {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #ffc107; margin: 15px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Refund Confirmed</h2>
        </div>
        <div class='content'>
            <p>Dear {studentName},</p>
            <p>Your refund has been processed successfully. Here are the details:</p>
            <div class='refund-info'>
                <p><strong>Refund Amount:</strong> {refundAmount}</p>
                <p><strong>Processing Date:</strong> {refundDate}</p>
            </div>
            <p>The refund will appear in your original payment method within 3-5 business days.</p>
            <p>If you have any questions, please contact our support team.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Refund confirmation sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send refund confirmation to {email}");
                throw;
            }
        }

        public async Task SendContractCancelledAsync(string email, string studentName, string reason, string cancellationDate)
        {
            try
            {
                var subject = "Contract Cancellation Confirmation - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #dc3545; padding: 20px; text-align: center; color: white; }}
        .content {{ padding: 20px; }}
        .cancel-info {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #dc3545; margin: 15px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Contract Cancelled</h2>
        </div>
        <div class='content'>
            <p>Dear {studentName},</p>
            <p>Your tutoring contract has been cancelled. Here are the details:</p>
            <div class='cancel-info'>
                <p><strong>Cancellation Date:</strong> {cancellationDate}</p>
                <p><strong>Reason:</strong> {reason}</p>
            </div>
            <p>We appreciate your time with MathBridge. If you would like to know more about our services or have any feedback, please reach out.</p>
            <p>Best regards,<br/>The MathBridge Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Contract cancellation notice sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send contract cancellation notice to {email}");
                throw;
            }
        }

        private async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new Message
                {
                    Raw = Base64UrlEncode(CreateRawMessage(recipientEmail, subject, htmlBody))
                };

                var request = _gmailService.Users.Messages.Send(message, "me");
                var result = await request.ExecuteAsync();
                _logger.LogInformation($"Email sent successfully. Message ID: {result.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {recipientEmail}");
                throw;
            }
        }

        private string CreateRawMessage(string to, string subject, string htmlBody)
        {
            var message = $"From: <{_fromEmail}>\r\n" +
                         $"To: <{to}>\r\n" +
                         $"Subject: {subject}\r\n" +
                         $"MIME-Version: 1.0\r\n" +
                         $"Content-Type: text/html; charset=utf-8\r\n" +
                         $"\r\n" +
                         htmlBody;
            return message;
        }

        private string Base64UrlEncode(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(inputBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
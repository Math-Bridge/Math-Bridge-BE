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
        private readonly string _fromEmail;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private GmailService _gmailService;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _logger = logger;
            _configuration = configuration;
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

        private ICredential CreateCredential()
        {
            var credentialsPath = _configuration["GoogleMeet:OAuthCredentialsPath"]
                ?? throw new ArgumentNullException("GoogleMeet:OAuthCredentialsPath");
            var userEmail = _configuration["GoogleMeet:WorkspaceUserEmail"]
                ?? throw new ArgumentNullException("GoogleMeet:WorkspaceUserEmail");
            
            var googleCredential = GoogleCredential.FromFile(credentialsPath);
            var originalCredential = googleCredential.UnderlyingCredential as ServiceAccountCredential;
            
            if (originalCredential == null)
            {
                throw new InvalidOperationException("The credential file does not contain a service account credential");
            }
            
            var scopes = new[]
            {
                "https://www.googleapis.com/auth/gmail.send",
                "https://www.googleapis.com/auth/gmail.settings.sharing"
            };
            
            var initializer = new ServiceAccountCredential.Initializer(
                originalCredential.Id,
                "https://oauth2.googleapis.com/token")
            {
                User = userEmail,
                Key = originalCredential.Key,
                KeyId = originalCredential.KeyId,
                Scopes = scopes
            };
            
            var delegatedCredential = new ServiceAccountCredential(initializer);
            _logger.LogInformation($"Gmail service initialized with delegation to {userEmail}");
            return delegatedCredential;
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
                // Format amount as VND currency
                var formattedAmount = amount;
                if (decimal.TryParse(amount.Replace(",", "").Replace(".", ""), out var amountValue))
                {
                    formattedAmount = string.Format(new System.Globalization.CultureInfo("vi-VN"), "{0:N0} VND", amountValue);
                }
                else if (!amount.Contains("VND"))
                {
                    formattedAmount = "{amount} VND";
                }

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
                <p><strong>Amount Due:</strong> <span class='amount'>{formattedAmount}</span></p>
                <p><strong>Due Date:</strong> {dueDate}</p>
            </div>
            <p>Thank you for choosing MathBridge!</p>
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
            var message = $"From: {_fromEmail}\r\n" +
                         $"To: {to}\r\n" +
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
        public async Task SendContractConfirmationAsync(
    string email,
    string parentName,
    Guid contractId,
    byte[] pdfBytes,
    string pdfFileName = "MathBridge_Contract.pdf")
        {
            try
            {
                var subject = "Your MathBridge Tutoring Contract is Active!";
                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px; }}
        .content {{ padding: 20px; background-color: #f9f9f9; border-radius: 8px; margin-top: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #777; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Contract Activated Successfully!</h2>
        </div>
        <div class='content'>
            <p>Dear <strong>{parentName}</strong>,</p>
            <p>We're excited to confirm that your tutoring contract has been <strong>activated</strong>!</p>
            <p>Your child is now officially enrolled in the MathBridge learning program.</p>
            <p><strong>Contract ID:</strong> {contractId}</p>
            <p>Please find your official contract attached as a PDF. You can download and save it for your records.</p>
            <p style='text-align: center; margin: 25px 0;'>
                <a href='#' class='button'>View Contract in Browser</a>
            </p>
            <p>If you have any questions, our support team is always here to help.</p>
            <p>Thank you for trusting MathBridge with your child's education!</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} MathBridge System. All rights reserved.</p>
            <p>Email: <a href='mailto:{_fromEmail}'>{_fromEmail}</a></p>
        </div>
    </div>
</body>
</html>";

                await SendEmailWithAttachmentAsync(email, subject, htmlBody, pdfBytes, pdfFileName);
                _logger.LogInformation($"Contract confirmation email sent to {email} with PDF attachment.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send contract confirmation to {email}");
                throw;
            }
        }

        public async Task SendReportSubmittedAsync(string email, string parentName, Guid reportId)
        {
            try
            {
                var subject = "Report Submitted Successfully - MathBridge";
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
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Report Submitted</h2>
        </div>
        <div class='content'>
            <p>Dear {parentName},</p>
            <p>We have received your report (ID: {reportId}). Our support team will review it shortly.</p>
            <p>Thank you for helping us improve MathBridge.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Report submitted email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send report submitted email to {email}");
                throw;
            }
        }

        public async Task SendReportStatusUpdateAsync(string email, string parentName, Guid reportId, string status, string reason)
        {
            try
            {
                var subject = $"Report Status Update - {status} - MathBridge";
                var color = status.ToLower() == "approved" ? "#28a745" : (status.ToLower() == "denied" ? "#dc3545" : "#007bff");
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: {{color}}; padding: 20px; text-align: center; color: white; }}
        .content {{ padding: 20px; }}
        .reason-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid {{color}}; margin: 15px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Report Status: {status}</h2>
        </div>
        <div class='content'>
            <p>Dear {parentName},</p>
            <p>The status of your report (ID: {reportId}) has been updated to <strong>{status}</strong>.</p>
            <div class='reason-box'>
                <p><strong>Reason/Comments:</strong></p>
                <p>{reason}</p>
            </div>
            <p>If you have further questions, please contact support.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Report status update email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send report status update email to {email}");
                throw;
            }
        }

        public async Task SendRescheduleRequestCreatedAsync(string email, string parentName, string childName, string originalDate, string originalTime, string requestedDate, string requestedTime, string reason)
        {
            try
            {
                var subject = "Reschedule Request Submitted - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #17a2b8; padding: 20px; text-align: center; color: white; border-radius: 5px 5px 0 0; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #17a2b8; margin: 15px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Reschedule Request Submitted</h2>
        </div>
        <div class='content'>
            <p>Dear {parentName},</p>
            <p>Your reschedule request for <strong>{childName}</strong>'s tutoring session has been submitted successfully.</p>
            <div class='info-box'>
                <p><strong>Original Session:</strong></p>
                <p>Date: {originalDate}</p>
                <p>Time: {originalTime}</p>
                <br/>
                <p><strong>Requested New Schedule:</strong></p>
                <p>Date: {requestedDate}</p>
                <p>Time: {requestedTime}</p>
                <br/>
                <p><strong>Reason:</strong> {reason}</p>
            </div>
            <p>Our staff will review your request and notify you once it has been processed.</p>
            <p>Thank you for using MathBridge!</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Reschedule request created email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send reschedule request created email to {email}");
                throw;
            }
        }

        public async Task SendRescheduleApprovedAsync(string email, string parentName, string childName, string newDate, string newTime, string tutorName)
        {
            try
            {
                var subject = "Reschedule Request Approved - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #28a745; padding: 20px; text-align: center; color: white; border-radius: 5px 5px 0 0; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #28a745; margin: 15px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Reschedule Request Approved!</h2>
        </div>
        <div class='content'>
            <p>Dear {parentName},</p>
            <p>Great news! Your reschedule request for <strong>{childName}</strong>'s tutoring session has been <strong>approved</strong>.</p>
            <div class='info-box'>
                <p><strong>New Session Details:</strong></p>
                <p>Date: {newDate}</p>
                <p>Time: {newTime}</p>
                <p>Tutor: {tutorName}</p>
            </div>
            <p>Please make sure your child is ready for the session at the scheduled time.</p>
            <p>Thank you for choosing MathBridge!</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Reschedule approved email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send reschedule approved email to {email}");
                throw;
            }
        }

        public async Task SendRescheduleRejectedAsync(string email, string parentName, string childName, string originalDate, string originalTime, string reason)
        {
            try
            {
                var subject = "Reschedule Request Rejected - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #dc3545; padding: 20px; text-align: center; color: white; border-radius: 5px 5px 0 0; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #dc3545; margin: 15px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Reschedule Request Rejected</h2>
        </div>
        <div class='content'>
            <p>Dear {parentName},</p>
            <p>We regret to inform you that your reschedule request for <strong>{childName}</strong>'s tutoring session has been <strong>rejected</strong>.</p>
            <div class='info-box'>
                <p><strong>Original Session (remains unchanged):</strong></p>
                <p>Date: {originalDate}</p>
                <p>Time: {originalTime}</p>
                <br/>
                <p><strong>Reason for rejection:</strong> {reason}</p>
            </div>
            <p>Please ensure your child attends the original scheduled session. If you have any questions, please contact our support team.</p>
            <p>Thank you for your understanding.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Reschedule rejected email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send reschedule rejected email to {email}");
                throw;
            }
        }

        private async Task SendEmailWithAttachmentAsync(string to, string subject, string htmlBody, byte[] attachmentBytes, string attachmentName)
        {
            try
            {
                var message = new Message();
                var boundary = $"--boundary_{Guid.NewGuid()}";

                var content = new StringBuilder();
                content.AppendLine($"From: {_fromEmail}");
                content.AppendLine($"To: {to}");
                content.AppendLine($"Subject: {subject}");
                content.AppendLine("MIME-Version: 1.0");
                content.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
                content.AppendLine();
                content.AppendLine($"--{boundary}");
                content.AppendLine("Content-Type: text/html; charset=UTF-8");
                content.AppendLine();
                content.AppendLine(htmlBody);
                content.AppendLine();
                content.AppendLine($"--{boundary}");
                content.AppendLine($"Content-Type: application/pdf; name=\"{attachmentName}\"");
                content.AppendLine("Content-Transfer-Encoding: base64");
                content.AppendLine($"Content-Disposition: attachment; filename=\"{attachmentName}\"");
                content.AppendLine();
                content.AppendLine(Convert.ToBase64String(attachmentBytes));
                content.AppendLine($"--{boundary}--");

                message.Raw = Base64UrlEncode(content.ToString());
                var result = await _gmailService.Users.Messages.Send(message, "me").ExecuteAsync();
                _logger.LogInformation($"Email with attachment sent. Message ID: {result.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email with attachment to {to}");
                throw;
            }
        }

        public async Task SendWithdrawalRequestCreatedAsync(string email, string parentName, decimal amount, string bankName, string bankAccountNumber, DateTime requestDate)
        {
            try
            {
                var subject = "Withdrawal Request Submitted - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #17a2b8; padding: 20px; text-align: center; color: white; border-radius: 5px 5px 0 0; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #17a2b8; margin: 15px 0; }}
        .amount {{ font-size: 24px; color: #17a2b8; font-weight: bold; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Withdrawal Request Submitted</h2>
        </div>
        <div class='content'>
            <p>Dear {parentName},</p>
            <p>Your withdrawal request has been submitted successfully and is pending review.</p>
            <div class='info-box'>
                <p><strong>Amount:</strong> <span class='amount'>{amount:N0} VND</span></p>
                <p><strong>Bank Name:</strong> {bankName}</p>
                <p><strong>Account Number:</strong> {bankAccountNumber}</p>
                <p><strong>Request Date:</strong> {requestDate:dd/MM/yyyy HH:mm}</p>
                <p><strong>Status:</strong> Pending</p>
            </div>
            <p>Our staff will process your request shortly. You will receive another email once the withdrawal has been processed.</p>
            <p>Thank you for using MathBridge!</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Withdrawal request created email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send withdrawal request created email to {email}");
                throw;
            }
        }

        public async Task SendWithdrawalProcessedAsync(string email, string parentName, decimal amount, string bankName, string bankAccountNumber, DateTime processedDate)
        {
            try
            {
                var subject = "Withdrawal Processed Successfully - MathBridge";
                var htmlBody = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #28a745; padding: 20px; text-align: center; color: white; border-radius: 5px 5px 0 0; }}
        .content {{ padding: 20px; }}
        .info-box {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #28a745; margin: 15px 0; }}
        .amount {{ font-size: 24px; color: #28a745; font-weight: bold; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Withdrawal Processed</h2>
        </div>
        <div class='content'>
            <p>Dear {parentName},</p>
            <p>Great news! Your withdrawal request has been processed successfully.</p>
            <div class='info-box'>
                <p><strong>Amount:</strong> <span class='amount'>{amount:N0} VND</span></p>
                <p><strong>Bank Name:</strong> {bankName}</p>
                <p><strong>Account Number:</strong> {bankAccountNumber}</p>
                <p><strong>Processed Date:</strong> {processedDate:dd/MM/yyyy HH:mm}</p>
                <p><strong>Status:</strong> Completed</p>
            </div>
            <p>The funds have been transferred to your bank account. Please allow 1-3 business days for the transfer to appear in your account.</p>
            <p>Thank you for using MathBridge!</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} MathBridgeSystem. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await SendEmailAsync(email, subject, htmlBody);
                _logger.LogInformation($"Withdrawal processed email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send withdrawal processed email to {email}");
                throw;
            }
        }
    }
}
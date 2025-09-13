using MailKit.Net.Smtp;
using MathBridge.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Threading.Tasks;

namespace MathBridge.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _appPassword;
        private readonly string _fromEmail;

        public EmailService(IConfiguration configuration)
        {
            _host = configuration["Smtp:Host"] ?? throw new System.ArgumentNullException("Smtp:Host");
            _port = int.Parse(configuration["Smtp:Port"] ?? throw new System.ArgumentNullException("Smtp:Port"));
            _username = configuration["Smtp:Username"] ?? throw new System.ArgumentNullException("Smtp:Username");
            _appPassword = configuration["Smtp:AppPassword"] ?? throw new System.ArgumentNullException("Smtp:AppPassword");
            _fromEmail = configuration["Smtp:FromEmail"] ?? throw new System.ArgumentNullException("Smtp:FromEmail");
        }

        public async Task SendVerificationCodeAsync(string email, string code)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("MathBridge Support", _fromEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Your MathBridge Verification Code";

            // Sử dụng HTML body để thiết kế đẹp và lịch sự
            var htmlBody = new TextPart("html")
            {
                Text = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }}
        .header {{ background-color: #f8f9fa; padding: 10px; text-align: center; border-bottom: 1px solid #e0e0e0; }}
        .code {{ font-size: 24px; color: #2c3e50; font-weight: bold; text-align: center; padding: 10px; background-color: #ecf0f1; border-radius: 5px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #7f8c8d; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>MathBridge</h2>
            <p>Welcome to Your Learning Journey!</p>
        </div>
        <div style='padding: 20px;'>
            <p>Dear User,</p>
            <p>Thank you for registering with MathBridge. To complete your account setup, please use the verification code below:</p>
            <div class='code'>{code}</div>
            <p>This code will expire in <strong>10 minutes</strong>. Please enter it on the verification page to proceed.</p>
            <p>If you did not request this code, please ignore this email or contact our support team immediately.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MathBridge. All rights reserved.</p>
            <p>For assistance, email us at <a href='mailto:{_fromEmail}'>{_fromEmail}</a>.</p>
        </div>
    </div>
</body>
</html>"
            };
            message.Body = htmlBody;

            using var client = new SmtpClient();
            await client.ConnectAsync(_host, _port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_username, _appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
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
            message.From.Add(new MailboxAddress("MathBridge", _fromEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Xác thực đăng ký MathBridge";
            message.Body = new TextPart("plain")
            {
                Text = $"Mã xác thực của bạn là: {code}. Mã hết hạn sau 10 phút."
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_host, _port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_username, _appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
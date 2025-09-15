using System.Threading.Tasks;

namespace MathBridge.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationLinkAsync(string email, string link);
        Task SendResetPasswordLinkAsync(string email, string link);
    }
}
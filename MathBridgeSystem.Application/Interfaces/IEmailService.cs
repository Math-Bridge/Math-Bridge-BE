using System.Threading.Tasks;

namespace MathBridge.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string email, string code);
    }
}
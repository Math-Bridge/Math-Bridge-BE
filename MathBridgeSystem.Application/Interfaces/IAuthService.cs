using MathBridge.Application.DTOs;

namespace MathBridge.Application.Interfaces
{
    public interface IAuthService
    {
        Task<String> RegisterAsync(RegisterRequest request);
        Task<string> LoginAsync(LoginRequest request);
        Task<string> GoogleLoginAsync(string googleToken);
        Task<Guid> VerifyEmailLinkAsync(string oobCode, string token);
    }
}
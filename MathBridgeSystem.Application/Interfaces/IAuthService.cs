using MathBridge.Application.DTOs;

namespace MathBridge.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterRequest request);
        Task<string> LoginAsync(LoginRequest request);
        Task<string> GoogleLoginAsync(string googleToken);
        Task<Guid> VerifyEmailAsync(string oobCode);
        Task<string> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<string> ResetPasswordAsync(ResetPasswordRequest request);
        Task<string> ChangePasswordAsync(ChangePasswordRequest request, Guid userId);
    }
}
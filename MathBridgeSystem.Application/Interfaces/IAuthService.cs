using MathBridgeSystem.Application.DTOs;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> GoogleLoginAsync(string googleToken);
        Task<Guid> VerifyEmailAsync(string oobCode);
        Task<string> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<string> ResetPasswordAsync(ResetPasswordRequest request);
        Task<string> ChangePasswordAsync(ChangePasswordRequest request, Guid userId);
        Task<string> ResendVerificationAsync(ResendVerificationRequest request);
        Task<LoginResponse> RefreshTokenAsync(Guid userId);
    }
}
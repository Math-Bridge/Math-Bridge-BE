namespace MathBridge.Application.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<(string Email, string Name)> ValidateGoogleTokenAsync(string token);
    }
}
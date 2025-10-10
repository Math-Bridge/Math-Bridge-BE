namespace MathBridgeSystem.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(Guid userId, string role);
    }
}
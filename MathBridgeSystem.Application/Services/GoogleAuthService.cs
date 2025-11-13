using MathBridgeSystem.Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly FirebaseAuth _firebaseAuth;

        public GoogleAuthService()
        {
            _firebaseAuth = FirebaseAuth.DefaultInstance;
        }

        public async Task<(string Email, string Name)> ValidateGoogleTokenAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentException("Google id token is required", nameof(idToken));

            try
            {
                var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(idToken);
                var email = decodedToken.Claims.GetValueOrDefault("email")?.ToString();
                var name = decodedToken.Claims.GetValueOrDefault("name")?.ToString();

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
                    throw new Exception("Invalid Google token claims");

                return (email, name);
            }
            catch (FirebaseAuthException ex)
            {
                throw new Exception($"Google token validation failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Wrap any other exception to provide consistent message for callers/tests
                throw new Exception($"Google token validation failed: {ex.Message}");
            }
        }
    }
}
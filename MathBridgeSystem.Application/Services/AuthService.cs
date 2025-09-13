using BCrypt.Net;
using FirebaseAdmin.Auth;
using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using MathBridge.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace MathBridge.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, IGoogleAuthService googleAuthService, IEmailService emailService, IMemoryCache cache)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new Exception("Email already exists");

            // Hardcode RoleId to 3 (parent role) for all registrations
            const int parentRoleId = 3;
            var roleExists = await _userRepository.RoleExistsAsync(parentRoleId);
            if (!roleExists)
                throw new Exception($"Parent role (ID: {parentRoleId}) not found in database");

            // Create and save a temporary token in cache with user data
            var tempToken = Guid.NewGuid().ToString();
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
            };
            var cachedRequest = new
            {
                request.FullName,
                request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                request.PhoneNumber,
                request.Gender,
                RoleId = parentRoleId
            };
            _cache.Set(tempToken, cachedRequest, cacheEntryOptions);

            // Generate custom token 
            var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync("temp_" + request.Email);  

            // Link verify with custom token 
            var link = $"https://api.vibe88.tech/api/auth/verify-link?token={tempToken}&idToken={customToken}"; 

            await _emailService.SendVerificationLinkAsync(request.Email, link);

            return "Verification link sent to your email. Please check and click to complete registration.";
        }

        public async Task<Guid> VerifyEmailLinkAsync(string idToken, string token)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(token))
                throw new ArgumentException("Invalid verification token");

            // Verify custom token form Firebase 
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            var email = decodedToken.Uid;  

            if (!decodedToken.Uid.StartsWith("temp_") || decodedToken.Uid != "temp_" + email)
                throw new Exception("Invalid verification token");

            if (!_cache.TryGetValue(token, out var cachedRequest) || cachedRequest == null)
                throw new Exception("Invalid or expired registration request");

            var request = (dynamic)cachedRequest;
            if (request.Email != email)
                throw new Exception("Email mismatch in verification");

            // Check duplicate email
            if (await _userRepository.EmailExistsAsync(email))
                throw new Exception("Email already registered");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                RoleId = request.RoleId,
                WalletBalance = 0.00m,
                CreatedDate = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                Status = "active"
            };

            await _userRepository.AddAsync(user);
            _cache.Remove(token); 

            return user.UserId;
        }

        public async Task<string> LoginAsync(LoginRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            if (user.Status == "banned")
                throw new Exception("Account is banned");

            user.LastActive = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return _tokenService.GenerateJwtToken(user.UserId, user.Role.RoleName);
        }

        public async Task<string> GoogleLoginAsync(string googleToken)
        {
            if (string.IsNullOrEmpty(googleToken))
                throw new ArgumentException("Google token is required", nameof(googleToken));

            var (email, name) = await _googleAuthService.ValidateGoogleTokenAsync(googleToken);
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // Hardcode RoleId to 3 (parent role) for Google login
                const int parentRoleId = 3;
                var roleExists = await _userRepository.RoleExistsAsync(parentRoleId);
                if (!roleExists)
                    throw new Exception($"Parent role (ID: {parentRoleId}) not found in database");

                user = new User
                {
                    UserId = Guid.NewGuid(),
                    FullName = name,
                    Email = email,
                    PasswordHash = null,
                    PhoneNumber = "N/A",
                    Gender = "other",
                    RoleId = parentRoleId,
                    WalletBalance = 0.00m,
                    CreatedDate = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow,
                    Status = "active"
                };
                await _userRepository.AddAsync(user);
            }
            else
            {
                if (user.Status == "banned")
                    throw new Exception("Account is banned");

                user.LastActive = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            return _tokenService.GenerateJwtToken(user.UserId, user.Role.RoleName);
        }
    }
}
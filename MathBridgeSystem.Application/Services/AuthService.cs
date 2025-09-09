using BCrypt.Net;
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
        private string GenerateVerificationCode()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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

            var verificationCode = GenerateVerificationCode();
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            _cache.Set(request.Email + "_code", verificationCode, cacheEntryOptions);
            _cache.Set(request.Email + "_request", request, cacheEntryOptions); // save request
            await _emailService.SendVerificationCodeAsync(request.Email, verificationCode);

            return "Verification code sent to your email. Please verify to complete registration.";
            
        }
        public async Task<Guid> VerifyRegistrationAsync(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
                throw new ArgumentException("Email and code are required");

            if (!_cache.TryGetValue(email + "_code", out string cachedCode) || cachedCode != code)
                throw new Exception("Invalid or expired verification code");

            var request = _cache.Get<RegisterRequest>(email + "_request");
            if (request == null)
                throw new Exception("Registration data not found");

            _cache.Remove(email + "_code");
            _cache.Remove(email + "_request");

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
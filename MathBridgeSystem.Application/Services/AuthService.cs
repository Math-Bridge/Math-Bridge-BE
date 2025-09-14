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

            // Hardcode RoleId to 3 (parent role) for regular registrations
            const int parentRoleId = 3;
            var roleExists = await _userRepository.RoleExistsAsync(parentRoleId);
            if (!roleExists)
                throw new Exception($"Parent role (ID: {parentRoleId}) not found in database");

            // Generate OOB code
            var oobCode = Guid.NewGuid().ToString();
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) // Increase expiration time to 15 minutes
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
            _cache.Set(oobCode, cachedRequest, cacheEntryOptions);

            // Generate verification link with oobCode (sửa để đúng route API)
            var verificationLink = $"https://api.vibe88.tech/api/auth/verify-email?oobCode={oobCode}";

            try
            {
                await _emailService.SendVerificationLinkAsync(request.Email, verificationLink);
                Console.WriteLine($"Verification link sent to {request.Email}: {verificationLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send verification email: {ex.ToString()}");
                throw new Exception("Failed to send verification email", ex);
            }

            return "Verification link sent to your email. Please check and click to complete registration.";
        }

        public async Task<Guid> VerifyEmailAsync(string oobCode)
        {
            if (string.IsNullOrEmpty(oobCode))
            {
                Console.WriteLine("VerifyEmailAsync: OobCode is null or empty");
                throw new ArgumentException("Invalid verification code");
            }

            if (!_cache.TryGetValue(oobCode, out var cachedRequest) || cachedRequest == null)
            {
                Console.WriteLine($"VerifyEmailAsync: Invalid or expired oobCode: {oobCode}");
                throw new Exception("Invalid or expired verification code");
            }

            var request = (dynamic)cachedRequest;
            var email = request.Email;

            // Check duplicate email
            if (await _userRepository.EmailExistsAsync(email))
            {
                Console.WriteLine($"VerifyEmailAsync: Email already registered: {email}");
                throw new Exception("Email already registered");
            }

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = request.PasswordHash,
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                RoleId = request.RoleId,
                WalletBalance = 0.00m,
                CreatedDate = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                Status = "active"
            };

            try
            {
                await _userRepository.AddAsync(user);
                Console.WriteLine($"VerifyEmailAsync: User created successfully: {user.UserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VerifyEmailAsync: Failed to create user: {ex.ToString()}");
                throw new Exception("Failed to create user", ex);
            }

            _cache.Remove(oobCode);
            Console.WriteLine($"VerifyEmailAsync: Cache removed for oobCode: {oobCode}");

            return user.UserId;
        }

        public async Task<string> LoginAsync(LoginRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                Console.WriteLine($"LoginAsync: Invalid credentials for email: {request.Email}");
                throw new Exception("Invalid credentials");
            }

            if (user.Status == "banned")
            {
                Console.WriteLine($"LoginAsync: Account banned for email: {request.Email}");
                throw new Exception("Account is banned");
            }

            user.LastActive = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var token = _tokenService.GenerateJwtToken(user.UserId, user.Role.RoleName);
            Console.WriteLine($"LoginAsync: JWT token generated for user: {user.UserId}");
            return token;
        }

        public async Task<string> GoogleLoginAsync(string googleToken)
        {
            if (string.IsNullOrEmpty(googleToken))
            {
                Console.WriteLine("GoogleLoginAsync: Google token is null or empty");
                throw new ArgumentException("Google token is required", nameof(googleToken));
            }

            var (email, name) = await _googleAuthService.ValidateGoogleTokenAsync(googleToken);
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // Hardcode RoleId to 3 (parent role) for Google login
                const int parentRoleId = 3;
                var roleExists = await _userRepository.RoleExistsAsync(parentRoleId);
                if (!roleExists)
                {
                    Console.WriteLine($"GoogleLoginAsync: Parent role (ID: {parentRoleId}) not found");
                    throw new Exception($"Parent role (ID: {parentRoleId}) not found in database");
                }

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
                Console.WriteLine($"GoogleLoginAsync: New user created: {user.UserId}");
            }
            else
            {
                if (user.Status == "banned")
                {
                    Console.WriteLine($"GoogleLoginAsync: Account banned for email: {email}");
                    throw new Exception("Account is banned");
                }

                user.LastActive = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                Console.WriteLine($"GoogleLoginAsync: User updated: {user.UserId}");
            }

            var token = _tokenService.GenerateJwtToken(user.UserId, user.Role.RoleName);
            Console.WriteLine($"GoogleLoginAsync: JWT token generated for user: {user.UserId}");
            return token;
        }
    }
}
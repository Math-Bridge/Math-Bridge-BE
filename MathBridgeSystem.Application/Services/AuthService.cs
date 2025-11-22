using BCrypt.Net;
using FirebaseAdmin.Auth;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly FirebaseAuth _firebaseAuth;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, IGoogleAuthService googleAuthService, IEmailService emailService, IMemoryCache cache)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _firebaseAuth = FirebaseAuth.DefaultInstance;
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
            
            // Store email-to-oobCode mapping for resend functionality
            _cache.Set($"email_{request.Email}", oobCode, cacheEntryOptions);

            // Generate verification link with oobCode (sửa để đúng route API)
            var verificationLink = $"https://web.vibe88.tech/verify-email?oobCode={oobCode}";

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
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                LastActive = DateTime.UtcNow.ToLocalTime(),
                Status = "active"
            };

            try
            {
                await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
                {
                    Email = request.Email,
                    Disabled = false
                });
                Console.WriteLine($"VerifyEmailAsync: Firebase user created successfully for: {request.Email}");
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
            {
                // Ignore EMAIL_EXISTS error - user already exists in Firebase
                Console.WriteLine($"VerifyEmailAsync: Firebase user already exists for: {request.Email}, continuing with local registration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VerifyEmailAsync: Unexpected error creating Firebase user: {ex.ToString()}");
                throw new Exception("Failed to create user in Firebase", ex);
            }

            // Add user to local database
            await _userRepository.AddAsync(user);
            Console.WriteLine($"VerifyEmailAsync: User created successfully: {user.UserId}");

            _cache.Remove(oobCode);
            _cache.Remove($"email_{email}"); // Remove email mapping
            Console.WriteLine($"VerifyEmailAsync: Cache removed for oobCode: {oobCode}");

            return user.UserId;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
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

            user.LastActive = DateTime.UtcNow.ToLocalTime();
            await _userRepository.UpdateAsync(user);

            var token = _tokenService.GenerateJwtToken(user.UserId, user.Role.RoleName);
            Console.WriteLine($"LoginAsync: JWT token generated for user: {user.UserId}");
            return new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                Role = user.Role.RoleName,
                RoleId = user.Role.RoleId
            };
        }

        public async Task<LoginResponse> GoogleLoginAsync(string googleToken)
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
                var roleExists = await _userRepository.GetRoleByIdAsync(parentRoleId);
                if (roleExists.RoleId != parentRoleId)
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
                    Role = roleExists,
                    WalletBalance = 0.00m,
                    CreatedDate = DateTime.UtcNow.ToLocalTime(),
                    LastActive = DateTime.UtcNow.ToLocalTime(),
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

                user.LastActive = DateTime.UtcNow.ToLocalTime();
                await _userRepository.UpdateAsync(user);
                Console.WriteLine($"GoogleLoginAsync: User updated: {user.UserId}");
            }

            var token = _tokenService.GenerateJwtToken(user.UserId, user.Role.RoleName);
            Console.WriteLine($"GoogleLoginAsync: JWT token generated for user: {user.UserId}, token: {token}");
            
            
            return new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                Role = user.Role.RoleName,
                RoleId = user.Role.RoleId
            };
        }
        public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                await _firebaseAuth.GetUserByEmailAsync(request.Email);
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"ForgotPasswordAsync: Email not found in Firebase: {request.Email}, Error: {ex.Message}");
                throw new Exception("Email not registered in the system");
            }
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new Exception("Email not found");

            var oobCode = Guid.NewGuid().ToString();
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            };

            var cachedRequest = new
            {
                Email = request.Email
            };
            _cache.Set(oobCode, cachedRequest, cacheEntryOptions);

            var resetLink = $"https://web.vibe88.tech/verify-reset?oobCode={oobCode}";

            try
            {
                await _emailService.SendResetPasswordLinkAsync(request.Email, resetLink);
                Console.WriteLine($"Reset password link sent to {request.Email}: {resetLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reset password email: {ex.ToString()}");
                throw new Exception("Failed to send reset password email", ex);
            }

            return "Reset password link sent to your email. Please check and click to proceed.";
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.OobCode))
                throw new ArgumentException("Invalid reset code");

            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters");

            if (!_cache.TryGetValue(request.OobCode, out var cachedRequest) || cachedRequest == null)
                throw new Exception("Invalid or expired reset code");

            var cached = (dynamic)cachedRequest;
            var email = cached.Email;

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("User not found");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.LastActive = DateTime.UtcNow.ToLocalTime();
            await _userRepository.UpdateAsync(user);

            _cache.Remove(request.OobCode);
            Console.WriteLine($"ResetPasswordAsync: Password reset successfully for user: {user.UserId}");

            return "Password reset successfully. You can now login with your new password.";
        }
        public async Task<string> ChangePasswordAsync(ChangePasswordRequest request, Guid userId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                Console.WriteLine($"ChangePasswordAsync: Invalid current password for user: {user.UserId}");
                throw new Exception("Current password is incorrect");
            }

            // Validate new password format (already handled by DataAnnotations, but explicit check for consistency)
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$"))
                throw new ArgumentException("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.LastActive = DateTime.UtcNow.ToLocalTime();
            await _userRepository.UpdateAsync(user);

            Console.WriteLine($"ChangePasswordAsync: Password changed successfully for user: {user.UserId}");
            return "Password changed successfully.";
        }

        public async Task<string> ResendVerificationAsync(ResendVerificationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Check if email already exists in the database
            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new Exception("Email already registered. Please login.");

            // Try to get the oobCode from email mapping
            if (!_cache.TryGetValue($"email_{request.Email}", out string oobCode) || string.IsNullOrEmpty(oobCode))
            {
                Console.WriteLine($"ResendVerificationAsync: No pending registration found for email: {request.Email}");
                throw new Exception("No pending registration found for this email. Please register again.");
            }

            // Get the cached registration data
            if (!_cache.TryGetValue(oobCode, out var cachedRequest) || cachedRequest == null)
            {
                Console.WriteLine($"ResendVerificationAsync: Registration data expired for email: {request.Email}");
                throw new Exception("Registration data has expired. Please register again.");
            }

            // Generate verification link with existing oobCode
            var verificationLink = $"https://api.vibe88.tech/api/auth/verify-email?oobCode={oobCode}";

            try
            {
                await _emailService.SendVerificationLinkAsync(request.Email, verificationLink);
                Console.WriteLine($"Verification link resent to {request.Email}: {verificationLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resend verification email: {ex.ToString()}");
                throw new Exception("Failed to resend verification email", ex);
            }

            return "Verification link has been resent to your email. Please check and click to complete registration.";
        }
    }
}
using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using MathBridge.Domain.Interfaces;
using BCrypt.Net;
using System;
using System.Threading.Tasks;

namespace MathBridge.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IGoogleAuthService _googleAuthService;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, IGoogleAuthService googleAuthService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
        }

        public async Task<Guid> RegisterAsync(RegisterRequest request)
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
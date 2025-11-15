using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class AuthServiceComprehensiveTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<ITokenService> _tokenService;
        private readonly Mock<IGoogleAuthService> _googleAuthService;
        private readonly Mock<IEmailService> _emailService;
        private readonly IMemoryCache _cache;
        private readonly AuthService _service;

        public AuthServiceComprehensiveTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _tokenService = new Mock<ITokenService>();
            _googleAuthService = new Mock<IGoogleAuthService>();
            _emailService = new Mock<IEmailService>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new AuthService(_userRepo.Object, _tokenService.Object, _googleAuthService.Object, _emailService.Object, _cache);
        }

        [Fact]
        public async Task RegisterAsync_NullRequest_ThrowsArgumentNullException()
        {
            await FluentActions.Invoking(() => _service.RegisterAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmail_ThrowsException()
        {
            var request = new RegisterRequest { Email = "existing@test.com", Password = "Pass@123", FullName = "Test User" };
            _userRepo.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(true);

            await FluentActions.Invoking(() => _service.RegisterAsync(request))
                .Should().ThrowAsync<Exception>().WithMessage("Email already exists");
        }

        [Fact]
        public async Task RegisterAsync_ParentRoleNotFound_ThrowsException()
        {
            var request = new RegisterRequest { Email = "new@test.com", Password = "Pass@123", FullName = "Test User", PhoneNumber = "123", Gender = "male" };
            _userRepo.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepo.Setup(r => r.RoleExistsAsync(3)).ReturnsAsync(false);

            await FluentActions.Invoking(() => _service.RegisterAsync(request))
                .Should().ThrowAsync<Exception>().WithMessage("Parent role (ID: 3) not found in database");
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_SendsVerificationEmail()
        {
            var request = new RegisterRequest { Email = "new@test.com", Password = "Pass@123", FullName = "Test User", PhoneNumber = "123", Gender = "male" };
            _userRepo.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepo.Setup(r => r.RoleExistsAsync(3)).ReturnsAsync(true);
            _emailService.Setup(e => e.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _service.RegisterAsync(request);

            result.Should().Contain("Verification link sent to your email");
            _emailService.Verify(e => e.SendVerificationLinkAsync(request.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_EmailServiceFails_ThrowsException()
        {
            var request = new RegisterRequest { Email = "new@test.com", Password = "Pass@123", FullName = "Test User", PhoneNumber = "123", Gender = "male" };
            _userRepo.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepo.Setup(r => r.RoleExistsAsync(3)).ReturnsAsync(true);
            _emailService.Setup(e => e.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("SMTP error"));

            await FluentActions.Invoking(() => _service.RegisterAsync(request))
                .Should().ThrowAsync<Exception>().WithMessage("Failed to send verification email");
        }

        [Fact]
        public async Task VerifyEmailAsync_NullOrEmptyOobCode_ThrowsArgumentException()
        {
            await FluentActions.Invoking(() => _service.VerifyEmailAsync(null!))
                .Should().ThrowAsync<ArgumentException>();

            await FluentActions.Invoking(() => _service.VerifyEmailAsync(""))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task VerifyEmailAsync_InvalidOobCode_ThrowsException()
        {
            await FluentActions.Invoking(() => _service.VerifyEmailAsync("invalid-code"))
                .Should().ThrowAsync<Exception>().WithMessage("Invalid or expired verification code");
        }





        [Fact]
        public async Task LoginAsync_NullRequest_ThrowsArgumentNullException()
        {
            await FluentActions.Invoking(() => _service.LoginAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsException()
        {
            var request = new LoginRequest { Email = "notfound@test.com", Password = "Pass@123" };
            _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User)null!);

            await FluentActions.Invoking(() => _service.LoginAsync(request))
                .Should().ThrowAsync<Exception>().WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task LoginAsync_BannedUser_ThrowsException()
        {
            var request = new LoginRequest { Email = "banned@test.com", Password = "Pass@123" };
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = "banned",
                Role = new Role { RoleName = "parent", RoleId = 3 }
            };
            _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(user);

            await FluentActions.Invoking(() => _service.LoginAsync(request))
                .Should().ThrowAsync<Exception>().WithMessage("Account is banned");
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
        {
            var request = new LoginRequest { Email = "valid@test.com", Password = "Pass@123" };
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = "active",
                Role = new Role { RoleName = "parent", RoleId = 3 }
            };
            _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(user);
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _tokenService.Setup(t => t.GenerateJwtToken(user.UserId, user.Role.RoleName)).Returns("fake-token");

            var result = await _service.LoginAsync(request);

            result.Should().NotBeNull();
            result.Token.Should().Be("fake-token");
            result.UserId.Should().Be(user.UserId);
            result.Role.Should().Be("parent");
        }

        [Fact]
        public async Task GoogleLoginAsync_NullOrEmptyToken_ThrowsArgumentException()
        {
            await FluentActions.Invoking(() => _service.GoogleLoginAsync(null!))
                .Should().ThrowAsync<ArgumentException>();

            await FluentActions.Invoking(() => _service.GoogleLoginAsync(""))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GoogleLoginAsync_NewUser_CreatesAccount()
        {
            var googleToken = "valid-google-token";
            _googleAuthService.Setup(g => g.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(("new@google.com", "New User"));
            _userRepo.Setup(r => r.GetByEmailAsync("new@google.com")).ReturnsAsync((User)null!);
            _userRepo.Setup(r => r.GetRoleByIdAsync(3)).ReturnsAsync(new Role { RoleId = 3, RoleName = "parent" });
            _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _tokenService.Setup(t => t.GenerateJwtToken(It.IsAny<Guid>(), "parent")).Returns("fake-token");

            var result = await _service.GoogleLoginAsync(googleToken);

            result.Should().NotBeNull();
            result.Token.Should().Be("fake-token");
            result.Role.Should().Be("parent");
            _userRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "new@google.com")), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_ExistingBannedUser_ThrowsException()
        {
            var googleToken = "valid-google-token";
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "banned@google.com",
                Status = "banned",
                Role = new Role { RoleName = "parent", RoleId = 3 }
            };
            _googleAuthService.Setup(g => g.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(("banned@google.com", "Banned User"));
            _userRepo.Setup(r => r.GetByEmailAsync("banned@google.com")).ReturnsAsync(user);

            await FluentActions.Invoking(() => _service.GoogleLoginAsync(googleToken))
                .Should().ThrowAsync<Exception>().WithMessage("Account is banned");
        }

        [Fact]
        public async Task GoogleLoginAsync_ExistingActiveUser_ReturnsToken()
        {
            var googleToken = "valid-google-token";
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "existing@google.com",
                Status = "active",
                Role = new Role { RoleName = "parent", RoleId = 3 }
            };
            _googleAuthService.Setup(g => g.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(("existing@google.com", "Existing User"));
            _userRepo.Setup(r => r.GetByEmailAsync("existing@google.com")).ReturnsAsync(user);
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _tokenService.Setup(t => t.GenerateJwtToken(user.UserId, user.Role.RoleName)).Returns("fake-token");

            var result = await _service.GoogleLoginAsync(googleToken);

            result.Should().NotBeNull();
            result.Token.Should().Be("fake-token");
            _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_NullRequest_ThrowsArgumentNullException()
        {
            await FluentActions.Invoking(() => _service.ForgotPasswordAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserNotFound_ThrowsException()
        {
            var request = new ForgotPasswordRequest { Email = "notfound@test.com" };
            _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User)null!);

            await FluentActions.Invoking(() => _service.ForgotPasswordAsync(request))
                .Should().ThrowAsync<Exception>();
        }



        [Fact]
        public async Task ResetPasswordAsync_NullRequest_ThrowsArgumentNullException()
        {
            await FluentActions.Invoking(() => _service.ResetPasswordAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ResetPasswordAsync_EmptyOobCode_ThrowsArgumentException()
        {
            var request = new ResetPasswordRequest { OobCode = "", NewPassword = "NewPass@123" };

            await FluentActions.Invoking(() => _service.ResetPasswordAsync(request))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ResetPasswordAsync_ShortPassword_ThrowsArgumentException()
        {
            var request = new ResetPasswordRequest { OobCode = "valid-code", NewPassword = "12345" };

            await FluentActions.Invoking(() => _service.ResetPasswordAsync(request))
                .Should().ThrowAsync<ArgumentException>().WithMessage("Password must be at least 6 characters");
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidOobCode_ThrowsException()
        {
            var request = new ResetPasswordRequest { OobCode = "invalid-code", NewPassword = "NewPass@123" };

            await FluentActions.Invoking(() => _service.ResetPasswordAsync(request))
                .Should().ThrowAsync<Exception>().WithMessage("Invalid or expired reset code");
        }



        [Fact]
        public async Task ChangePasswordAsync_NullRequest_ThrowsArgumentNullException()
        {
            await FluentActions.Invoking(() => _service.ChangePasswordAsync(null!, Guid.NewGuid()))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ChangePasswordAsync_UserNotFound_ThrowsException()
        {
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequest { CurrentPassword = "Old@123", NewPassword = "New@123" };
            _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null!);

            await FluentActions.Invoking(() => _service.ChangePasswordAsync(request, userId))
                .Should().ThrowAsync<Exception>().WithMessage("User not found");
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ThrowsException()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPassword@123")
            };
            _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var request = new ChangePasswordRequest { CurrentPassword = "Wrong@123", NewPassword = "New@123Abc!" };

            await FluentActions.Invoking(() => _service.ChangePasswordAsync(request, userId))
                .Should().ThrowAsync<Exception>().WithMessage("Current password is incorrect");
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidNewPasswordFormat_ThrowsArgumentException()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Old@123")
            };
            _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var request = new ChangePasswordRequest { CurrentPassword = "Old@123", NewPassword = "weak" };

            await FluentActions.Invoking(() => _service.ChangePasswordAsync(request, userId))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidRequest_ChangesPassword()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Old@123Abc!")
            };
            _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var request = new ChangePasswordRequest { CurrentPassword = "Old@123Abc!", NewPassword = "New@123Abc!" };
            var result = await _service.ChangePasswordAsync(request, userId);

            result.Should().Contain("Password changed successfully");
            _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.UserId == userId)), Times.Once);
        }
    }
}

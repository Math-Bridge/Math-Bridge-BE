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
    internal class CachedRegisterData
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }

    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IGoogleAuthService> _googleAuthServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly IMemoryCache _memoryCache;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _googleAuthServiceMock = new Mock<IGoogleAuthService>();
            _emailServiceMock = new Mock<IEmailService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _authService = new AuthService(
                _userRepositoryMock.Object,
                _tokenServiceMock.Object,
                _googleAuthServiceMock.Object,
                _emailServiceMock.Object,
                _memoryCache
            );
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_SendsVerificationLinkAndReturnsMessage()
        {
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "StrongPass1!",
                PhoneNumber = "1234567890",
                Gender = "male",
                RoleId = 3
            };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(repo => repo.RoleExistsAsync(3)).ReturnsAsync(true);
            _emailServiceMock.Setup(email => email.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _authService.RegisterAsync(request);

            result.Should().Be("Verification link sent to your email. Please check and click to complete registration.");
            _emailServiceMock.Verify(email => email.SendVerificationLinkAsync(request.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmail_ThrowsException()
        {
            var request = new RegisterRequest { Email = "existing@example.com" };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            Func<Task> act = () => _authService.RegisterAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Email already exists");
        }

        [Fact]
        public async Task RegisterAsync_InvalidRole_ThrowsException()
        {
            var request = new RegisterRequest { Email = "test@example.com" };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _userRepositoryMock.Setup(repo => repo.RoleExistsAsync(3)).ReturnsAsync(false);

            Func<Task> act = () => _authService.RegisterAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Parent role (ID: 3) not found in database");
        }

        [Fact]
        public async Task VerifyEmailAsync_ValidOobCode_CreatesUserAndReturnsUserId()
        {
            var oobCode = Guid.NewGuid().ToString();
            var cachedData = new Dictionary<string, object>
            {
                ["FullName"] = "Test User",
                ["Email"] = "test@example.com",
                ["PasswordHash"] = "hashed-pass",
                ["PhoneNumber"] = "1234567890",
                ["Gender"] = "male",
                ["RoleId"] = 3
            };
            _memoryCache.Set(oobCode, cachedData, TimeSpan.FromMinutes(15));
            _userRepositoryMock.Setup(r => r.EmailExistsAsync("test@example.com")).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _authService.VerifyEmailAsync(oobCode);

            result.Should().NotBe(Guid.Empty);
            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "test@example.com")), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_InvalidOobCode_ThrowsException()
        {
            var oobCode = "invalid-oob-code";
            Func<Task> act = () => _authService.VerifyEmailAsync(oobCode);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid or expired verification code");
        }

        [Fact]
        public async Task VerifyEmailAsync_ExistingEmailAfterCache_ThrowsException()
        {
            var oobCode = Guid.NewGuid().ToString();
            var cachedData = new Dictionary<string, object> { ["Email"] = "existing@example.com" };
            _memoryCache.Set(oobCode, cachedData, TimeSpan.FromMinutes(15));
            _userRepositoryMock.Setup(r => r.EmailExistsAsync("existing@example.com")).ReturnsAsync(true);

            var act = () => _authService.VerifyEmailAsync(oobCode);
            await act.Should().ThrowAsync<Exception>().WithMessage("Email already registered");
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
        {
            var request = new LoginRequest { Email = "test@example.com", Password = "correct-password" };
            var user = new User
            {
                UserId = Guid.NewGuid(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
                Role = new Role { RoleName = "parent", RoleId = 3 },
                Status = "active"
            };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(ts => ts.GenerateJwtToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("fake-jwt-token");

            var result = await _authService.LoginAsync(request);

            result.Token.Should().Be("fake-jwt-token");
            result.UserId.Should().Be(user.UserId);
            result.Role.Should().Be("parent");
            result.RoleId.Should().Be(3);
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<User>(u => u.LastActive > DateTime.MinValue)), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InvalidCredentials_ThrowsException()
        {
            var request = new LoginRequest { Email = "test@example.com", Password = "wrong-password" };
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password") };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email)).ReturnsAsync(user);

            Func<Task> act = () => _authService.LoginAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task LoginAsync_BannedUser_ThrowsException()
        {
            var request = new LoginRequest { Email = "banned@example.com", Password = "password" };
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), Status = "banned" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email)).ReturnsAsync(user);

            Func<Task> act = () => _authService.LoginAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Account is banned");
        }

        [Fact]
        public async Task GoogleLoginAsync_NewUser_CreatesUserAndReturnsToken()
        {
            var googleToken = "valid-google-token";
            var email = "newuser@gmail.com";
            var name = "New User";

            _googleAuthServiceMock.Setup(g => g.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync((email, name));
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User)null);
            _userRepositoryMock.Setup(r => r.RoleExistsAsync(3)).ReturnsAsync(true);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(t => t.GenerateJwtToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("jwt-token");

            var result = await _authService.GoogleLoginAsync(googleToken);

            result.Should().Be("jwt-token");
            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == email && u.FullName == name)), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_ExistingUser_UpdatesAndReturnsToken()
        {
            var googleToken = "valid-google-token";
            var user = new User { UserId = Guid.NewGuid(), Role = new Role { RoleName = "parent" }, Status = "active" };
            _googleAuthServiceMock.Setup(ga => ga.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(("existing@example.com", "Existing User"));
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(ts => ts.GenerateJwtToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("fake-jwt-token");

            var result = await _authService.GoogleLoginAsync(googleToken);

            result.Should().Be("fake-jwt-token");
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_BannedUser_ThrowsException()
        {
            var googleToken = "valid-google-token";
            var user = new User { Status = "banned" };
            _googleAuthServiceMock.Setup(ga => ga.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(("banned@example.com", "Banned User"));
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            Func<Task> act = () => _authService.GoogleLoginAsync(googleToken);
            await act.Should().ThrowAsync<Exception>().WithMessage("Account is banned");
        }

        [Fact]
        public async Task ForgotPasswordAsync_ValidEmail_SendsResetLink()
        {
            var email = "test@example.com";
            var user = new User { Email = email, UserId = Guid.NewGuid() }; // Thêm UserId
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
            _emailServiceMock.Setup(e => e.SendResetPasswordLinkAsync(email, It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });

            result.Should().Be("Reset password link sent to your email. Please check and click to proceed.");
            _emailServiceMock.Verify(e => e.SendResetPasswordLinkAsync(email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_NonExistingEmail_ThrowsException()
        {
            var email = "unknown@example.com";
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User)null);

            Func<Task> act = () => _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });
            await act.Should().ThrowAsync<Exception>().WithMessage("Email not found");
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidRequest_ResetsPassword()
        {
            var oobCode = Guid.NewGuid().ToString();
            var email = "test@example.com";
            var newPassword = "NewPass123!";
            var user = new User { UserId = Guid.NewGuid(), Email = email };

            _memoryCache.Set(oobCode, new Dictionary<string, object> { ["Email"] = email }, TimeSpan.FromMinutes(15));

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                OobCode = oobCode,
                NewPassword = newPassword
            });

            result.Should().Be("Password reset successfully. You can now login with your new password.");
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);

            var updatedUser = _userRepositoryMock.Invocations
                .First(i => i.Method.Name == "UpdateAsync")
                .Arguments[0] as User;

            BCrypt.Net.BCrypt.Verify(newPassword, updatedUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidOobCode_ThrowsException()
        {
            var request = new ResetPasswordRequest { OobCode = "invalid", NewPassword = "NewPass1!" };
            Func<Task> act = () => _authService.ResetPasswordAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid or expired reset code");
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidRequest_ChangesPassword()
        {
            var request = new ChangePasswordRequest { CurrentPassword = "OldPass1!", NewPassword = "NewPass1!" };
            var userId = Guid.NewGuid();
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _authService.ChangePasswordAsync(request, userId);

            result.Should().Be("Password changed successfully.");
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Once);

            var updatedUser = _userRepositoryMock.Invocations
                .First(i => i.Method.Name == "UpdateAsync")
                .Arguments[0] as User;

            BCrypt.Net.BCrypt.Verify("NewPass1!", updatedUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ThrowsException()
        {
            var request = new ChangePasswordRequest { CurrentPassword = "WrongPass", NewPassword = "NewPass1!" };
            var userId = Guid.NewGuid();
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            Func<Task> act = () => _authService.ChangePasswordAsync(request, userId);
            await act.Should().ThrowAsync<Exception>().WithMessage("Current password is incorrect");
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidNewPasswordFormat_ThrowsException()
        {
            var request = new ChangePasswordRequest { CurrentPassword = "OldPass1!", NewPassword = "weak" };
            var userId = Guid.NewGuid();
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            Func<Task> act = () => _authService.ChangePasswordAsync(request, userId);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*must contain at least one uppercase*");
        }
    }
}
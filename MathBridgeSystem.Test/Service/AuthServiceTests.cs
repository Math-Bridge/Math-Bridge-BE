using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _authService.RegisterAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_SendsVerificationLink()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "StrongPass1!",
                PhoneNumber = "1234567890",
                Gender = "male",
                RoleId = 3 // Service hardcode RoleId = 3, nhưng request vẫn có thể truyền (dù bị ghi đè hoặc dùng để check)
            };

            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(repo => repo.RoleExistsAsync(3)).ReturnsAsync(true); // Service hardcode check Role 3
            _emailServiceMock.Setup(email => email.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.RegisterAsync(request);

            // Assert
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
        public async Task RegisterAsync_ParentRoleNotFound_ThrowsException()
        {
            var request = new RegisterRequest { Email = "test@example.com" };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _userRepositoryMock.Setup(repo => repo.RoleExistsAsync(3)).ReturnsAsync(false); // Parent role (3) không tồn tại

            Func<Task> act = () => _authService.RegisterAsync(request);

            await act.Should().ThrowAsync<Exception>().WithMessage("Parent role (ID: 3) not found in database");
        }

        [Fact]
        public async Task RegisterAsync_EmailServiceFails_ThrowsException()
        {
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "StrongPass1!",
            };
            _userRepositoryMock.Setup(repo => repo.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(repo => repo.RoleExistsAsync(3)).ReturnsAsync(true);
            _emailServiceMock.Setup(email => email.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP error"));

            Func<Task> act = () => _authService.RegisterAsync(request);

            await act.Should().ThrowAsync<Exception>().WithMessage("Failed to send verification email");
        }

        #endregion

        #region VerifyEmailAsync Tests

        [Fact]
        public async Task VerifyEmailAsync_NullOrEmptyOobCode_ThrowsArgumentException()
        {
            Func<Task> actNull = () => _authService.VerifyEmailAsync(null);
            Func<Task> actEmpty = () => _authService.VerifyEmailAsync(string.Empty);

            await actNull.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid verification code");
            await actEmpty.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid verification code");
        }

        [Fact]
        public async Task VerifyEmailAsync_InvalidOobCode_ThrowsException()
        {
            var oobCode = "invalid-oob-code";

            Func<Task> act = () => _authService.VerifyEmailAsync(oobCode);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid or expired verification code");
        }

        // GHI CHÚ: Test này sẽ gọi FirebaseAuth.DefaultInstance.CreateUserAsync.
        // Vì không thể mock static method, test này sẽ thất bại trong môi trường unit test.
        //[Fact(Skip = "Không thể test do gọi static FirebaseAuth.DefaultInstance. Cần refactor service để inject FirebaseAuth.")]
        //public async Task VerifyEmailAsync_ValidOobCode_CreatesUser()
        //{
        //    // Arrange
        //    var oobCode = Guid.NewGuid().ToString();
        //    var cachedData = new
        //    {
        //        FullName = "Test User",
        //        Email = "test@example.com",
        //        PasswordHash = "hashed",
        //        PhoneNumber = "123",
        //        Gender = "Male",
        //        RoleId = 3
        //    };
        //    _memoryCache.Set(oobCode, cachedData);

        //    _userRepositoryMock.Setup(r => r.EmailExistsAsync("test@example.com")).ReturnsAsync(false);
        //    _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        //    // Act
        //    await _authService.VerifyEmailAsync(oobCode);

        //    // Assert (Sẽ không bao giờ chạy đến đây nếu chưa refactor)
        //}


        [Fact]
        public async Task VerifyEmailAsync_ExistingEmail_ThrowsException()
        {
            var oobCode = Guid.NewGuid().ToString();
            var cachedData = new { Email = "existing@example.com" };
            _memoryCache.Set(oobCode, cachedData);
            _userRepositoryMock.Setup(r => r.EmailExistsAsync("existing@example.com")).ReturnsAsync(true);

            var act = () => _authService.VerifyEmailAsync(oobCode);
            await act.Should().ThrowAsync<Exception>().WithMessage("Email already registered");
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _authService.LoginAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
        {
            // Arrange
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

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Token.Should().Be("fake-jwt-token");
            result.UserId.Should().Be(user.UserId);
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
        public async Task LoginAsync_UserNotFound_ThrowsException()
        {
            var request = new LoginRequest { Email = "notfound@example.com", Password = "password" };
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email)).ReturnsAsync((User)null);

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

        #endregion

        #region GoogleLoginAsync Tests

        [Fact]
        public async Task GoogleLoginAsync_NullOrEmptyToken_ThrowsArgumentException()
        {
            Func<Task> actNull = () => _authService.GoogleLoginAsync(null);
            Func<Task> actEmpty = () => _authService.GoogleLoginAsync(string.Empty);

            await actNull.Should().ThrowAsync<ArgumentException>().WithParameterName("googleToken");
            await actEmpty.Should().ThrowAsync<ArgumentException>().WithParameterName("googleToken");
        }

        [Fact]
        public async Task GoogleLoginAsync_GoogleAuthFails_ThrowsException()
        {
            _googleAuthServiceMock.Setup(g => g.ValidateGoogleTokenAsync("invalid-token"))
                .ThrowsAsync(new Exception("Invalid token"));

            Func<Task> act = () => _authService.GoogleLoginAsync("invalid-token");
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid token");
        }

        [Fact]
        public async Task GoogleLoginAsync_NewUser_CreatesUserAndReturnsToken()
        {
            // Arrange
            var googleToken = "valid-google-token";
            var email = "newuser@gmail.com";
            var name = "New User";

            _googleAuthServiceMock.Setup(g => g.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync((email, name));
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User)null); 
            _userRepositoryMock.Setup(r => r.RoleExistsAsync(3)).ReturnsAsync(true); 
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _tokenServiceMock.Setup(t => t.GenerateJwtToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("jwt-token");

            // Act
            var result = await _authService.GoogleLoginAsync(googleToken);

            // Assert
            result.Should().Be("jwt-token");
            // Kiểm tra user được tạo với đúng thông tin và RoleId = 3
            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == email &&
                u.FullName == name &&
                u.RoleId == 3 &&
                u.Gender == "other"
            )), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_NewUserParentRoleNotFound_ThrowsException()
        {
            var googleToken = "valid-google-token";
            _googleAuthServiceMock.Setup(g => g.ValidateGoogleTokenAsync(googleToken)).ReturnsAsync(("new@gmail.com", "New User"));
            _userRepositoryMock.Setup(r => r.GetByEmailAsync("new@gmail.com")).ReturnsAsync((User)null);
            _userRepositoryMock.Setup(r => r.RoleExistsAsync(3)).ReturnsAsync(false); // Role 3 không tồn tại

            Func<Task> act = () => _authService.GoogleLoginAsync(googleToken);
            await act.Should().ThrowAsync<Exception>().WithMessage("Parent role (ID: 3) not found in database");
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

        #endregion

        #region ForgotPasswordAsync Tests

        [Fact]
        public async Task ForgotPasswordAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _authService.ForgotPasswordAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // GHI CHÚ: Test này sẽ bị SKIPPED vì FirebaseAuth.DefaultInstance
        //[Fact(Skip = "Không thể test do gọi static FirebaseAuth.DefaultInstance. Cần refactor service.")]
        //public async Task ForgotPasswordAsync_ValidEmail_SendsResetLink()
        //{
        //    // Arrange
        //    var email = "test@example.com";
        //    var user = new User { Email = email };
        //    _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);

        //    // Act
        //    await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email });

        //    // Assert
        //    // ...
        //}

        #endregion

        #region ResetPasswordAsync Tests

        [Fact]
        public async Task ResetPasswordAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _authService.ResetPasswordAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidOobCode_ThrowsException()
        {
            var request = new ResetPasswordRequest { OobCode = "invalid", NewPassword = "NewPass1!" };
            Func<Task> act = () => _authService.ResetPasswordAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid or expired reset code");
        }

        [Fact]
        public async Task ResetPasswordAsync_PasswordTooShort_ThrowsArgumentException()
        {
            var request = new ResetPasswordRequest { OobCode = "code", NewPassword = "123" }; // < 6 chars
            Func<Task> act = () => _authService.ResetPasswordAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Password must be at least 6 characters");
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidRequest_ResetsPassword()
        {
            // Arrange
            var oobCode = Guid.NewGuid().ToString();
            var email = "test@example.com";
            var newPassword = "NewPass123!";
            var user = new User { UserId = Guid.NewGuid(), Email = email };

            // Set cache
            _memoryCache.Set(oobCode, new { Email = email }, TimeSpan.FromMinutes(15));

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                OobCode = oobCode,
                NewPassword = newPassword
            });

            // Assert
            result.Should().Be("Password reset successfully. You can now login with your new password.");
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);

            _memoryCache.TryGetValue(oobCode, out _).Should().BeFalse();
        }

        #endregion

        #region ChangePasswordAsync Tests

        [Fact]
        public async Task ChangePasswordAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _authService.ChangePasswordAsync(null, Guid.NewGuid());
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        [Fact]
        public async Task ChangePasswordAsync_UserNotFound_ThrowsException()
        {
            var request = new ChangePasswordRequest { CurrentPassword = "OldPass1!", NewPassword = "NewPass1!" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null);

            Func<Task> act = () => _authService.ChangePasswordAsync(request, Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidRequest_ChangesPassword()
        {
            // Arrange
            var request = new ChangePasswordRequest { CurrentPassword = "OldPass1!", NewPassword = "NewPass1!" };
            var userId = Guid.NewGuid();
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ChangePasswordAsync(request, userId);

            // Assert
            result.Should().Be("Password changed successfully.");
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ThrowsException()
        {
            var request = new ChangePasswordRequest { CurrentPassword = "WrongPass", NewPassword = "NewPass1!" };
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

            Func<Task> act = () => _authService.ChangePasswordAsync(request, Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Current password is incorrect");
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidNewPasswordFormat_ThrowsException()
        {
            var request = new ChangePasswordRequest { CurrentPassword = "OldPass1!", NewPassword = "weak" };
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

            Func<Task> act = () => _authService.ChangePasswordAsync(request, Guid.NewGuid());
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*must contain at least one uppercase*");
        }

        #endregion
    }
}
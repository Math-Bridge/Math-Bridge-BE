using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Application.DTOs;

namespace MathBridgeSystem.Test.Service.Advanced
{
    public class AuthServiceEdgeTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IRoleRepository> _roleRepo = new();
        private readonly Mock<IEmailService> _emailService = new();
        private readonly Mock<IGoogleAuthService> _googleAuth = new();
        private readonly Mock<ITokenService> _tokenService = new();
        private AuthService CreateService() => new AuthService(_userRepo.Object, new Mock<ITokenService>().Object, new Mock<IGoogleAuthService>().Object, _emailService.Object, new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

        [Fact]
        public async Task LoginAsync_BannedUser_Throws()
        {
            var dto = new LoginRequest{ Email="a@b.com", Password="Pass123!" };
            _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new User{ Email=dto.Email, PasswordHash=BCrypt.Net.BCrypt.HashPassword(dto.Password), Status="banned", Role = new Role{ RoleName="parent"}});
            var service = CreateService();
            await FluentActions.Invoking(()=> service.LoginAsync(dto)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task GoogleLoginAsync_Banned_Throws()
        {
            _googleAuth.Setup(g => g.ValidateGoogleTokenAsync("token")).ReturnsAsync(("mail@test.com","Name"));
            _userRepo.Setup(r => r.GetByEmailAsync("mail@test.com")).ReturnsAsync(new User{ Email="mail@test.com", Status="banned", Role = new Role{ RoleName="parent"}});
            var service = CreateService();
            await FluentActions.Invoking(()=> service.GoogleLoginAsync("token")).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrent_Throws()
        {
            var userId = Guid.NewGuid();
            _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User{ UserId=userId, PasswordHash=BCrypt.Net.BCrypt.HashPassword("Correct1!")});
            var service = CreateService();
            await FluentActions.Invoking(()=> service.ChangePasswordAsync(new ChangePasswordRequest{ CurrentPassword="Wrong", NewPassword="NewPass1!"}, userId)).Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidFormat_Throws()
        {
            var userId = Guid.NewGuid();
            _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User{ UserId=userId, PasswordHash=BCrypt.Net.BCrypt.HashPassword("Correct1!")});
            var service = CreateService();
            await FluentActions.Invoking(()=> service.ChangePasswordAsync(new ChangePasswordRequest{ CurrentPassword="Correct1!", NewPassword="short"}, userId)).Should().ThrowAsync<Exception>();
        }
    }
}

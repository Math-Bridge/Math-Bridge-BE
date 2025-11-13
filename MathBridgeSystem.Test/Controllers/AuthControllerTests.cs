using MathBridgeSystem.Presentation.Controllers;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using MathBridgeSystem.Application.DTOs;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;

namespace MathBridgeSystem.Tests.Controllers
{
    public class AuthControllerTests
    {
        [Fact]
        public async Task VerifyEmail_Get_BadRequest_WhenNoCode()
        {
            var authMock = new Mock<IAuthService>();
            var memory = new MemoryCache(new MemoryCacheOptions());
            var ctrl = new AuthController(authMock.Object, memory);

            var result = await ctrl.VerifyEmail(new VerifyEmailRequest { OobCode = "" });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task VerifyEmail_Get_ReturnsOk_WhenServiceSucceeds()
        {
            var authMock = new Mock<IAuthService>();
            authMock.Setup(a => a.VerifyEmailAsync(It.IsAny<string>())).ReturnsAsync(System.Guid.NewGuid());
            var memory = new MemoryCache(new MemoryCacheOptions());
            var ctrl = new AuthController(authMock.Object, memory);

            var result = await ctrl.VerifyEmail(new VerifyEmailRequest { OobCode = "abc" });
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task VerifyEmailGet_BadRequest_WhenNoCode()
        {
            var authMock = new Mock<IAuthService>();
            var memory = new MemoryCache(new MemoryCacheOptions());
            var ctrl = new AuthController(authMock.Object, memory);

            var result = await ctrl.VerifyEmailGet("");
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task VerifyResetGet_BadRequest_WhenNoCode()
        {
            var authMock = new Mock<IAuthService>();
            var memory = new MemoryCache(new MemoryCacheOptions());
            var ctrl = new AuthController(authMock.Object, memory);

            var result = await Task.Run(() => ctrl.VerifyResetGet(""));
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
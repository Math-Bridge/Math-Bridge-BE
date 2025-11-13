using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly IMemoryCache _memoryCache;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _controller = new AuthController(_authServiceMock.Object, _memoryCache);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullAuthService_ThrowsArgumentNullException()
        {
            Action act = () => new AuthController(null!, _memoryCache);
            act.Should().Throw<ArgumentNullException>().WithParameterName("authService");
        }

        [Fact]
        public void Constructor_NullMemoryCache_ThrowsArgumentNullException()
        {
            Action act = () => new AuthController(_authServiceMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_ValidRequest_ReturnsOkWithMessage()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "StrongPass1!",
                PhoneNumber = "1234567890",
                Gender = "male",
                RoleId = 3
            };
            var expectedMessage = "Verification link sent to your email";
            _authServiceMock.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(expectedMessage);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _authServiceMock.Verify(s => s.RegisterAsync(request), Times.Once);
        }

        [Fact]
        public async Task Register_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "StrongPass1!",
                PhoneNumber = "1234567890",
                Gender = "male",
                RoleId = 3
            };
            _authServiceMock.Setup(s => s.RegisterAsync(request))
                .ThrowsAsync(new Exception("Email already exists"));

            // Act
            var result = await _controller.Register(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region VerifyEmail Tests

        [Fact]
        public async Task VerifyEmail_ValidRequest_ReturnsOkWithUserId()
        {
            // Arrange
            var request = new VerifyEmailRequest { OobCode = "valid-code" };
            var expectedUserId = Guid.NewGuid();
            _authServiceMock.Setup(s => s.VerifyEmailAsync(request.OobCode))
                .ReturnsAsync(expectedUserId);

            // Act
            var result = await _controller.VerifyEmail(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _authServiceMock.Verify(s => s.VerifyEmailAsync(request.OobCode), Times.Once);
        }

        [Fact]
        public async Task VerifyEmail_EmptyOobCode_ReturnsBadRequest()
        {
            // Arrange
            var request = new VerifyEmailRequest { OobCode = "" };

            // Act
            var result = await _controller.VerifyEmail(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _authServiceMock.Verify(s => s.VerifyEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyEmail_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new VerifyEmailRequest { OobCode = "invalid-code" };
            _authServiceMock.Setup(s => s.VerifyEmailAsync(request.OobCode))
                .ThrowsAsync(new Exception("Invalid verification code"));

            // Act
            var result = await _controller.VerifyEmail(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task VerifyEmailGet_ValidOobCode_ReturnsOkWithMessage()
        {
            // Arrange
            var oobCode = "valid-code";
            var expectedUserId = Guid.NewGuid();
            _authServiceMock.Setup(s => s.VerifyEmailAsync(oobCode))
                .ReturnsAsync(expectedUserId);

            // Act
            var result = await _controller.VerifyEmailGet(oobCode);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _authServiceMock.Verify(s => s.VerifyEmailAsync(oobCode), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailGet_EmptyOobCode_ReturnsBadRequest()
        {
            // Arrange
            var oobCode = "";

            // Act
            var result = await _controller.VerifyEmailGet(oobCode);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _authServiceMock.Verify(s => s.VerifyEmailAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ValidRequest_ReturnsOkWithLoginResponse()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "StrongPass1!"
            };
            var expectedResponse = new LoginResponse
            {
                UserId = Guid.NewGuid(),
                Token = "jwt-token",
                Role = "parent",
                RoleId = 3
            };
            _authServiceMock.Setup(s => s.LoginAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _authServiceMock.Verify(s => s.LoginAsync(request), Times.Once);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };
            _authServiceMock.Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new Exception("Invalid credentials"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion
    }
}


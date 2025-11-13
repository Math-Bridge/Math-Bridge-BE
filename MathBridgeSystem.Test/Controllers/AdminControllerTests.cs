using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Presentation.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly AdminController _controller;
        private readonly Guid _adminUserId = Guid.NewGuid();

        public AdminControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _controller = new AdminController(_userServiceMock.Object);
            
            // Set up controller context with claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _adminUserId.ToString()),
                new Claim(ClaimTypes.Role, "admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullUserService_ThrowsArgumentNullException()
        {
            Action act = () => new AdminController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("userService");
        }

        #endregion

        #region CreateUser Tests

        [Fact]
        public async Task CreateUser_ValidRequest_ReturnsOkResultWithUserId()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "New User",
                Email = "newuser@example.com",
                Password = "StrongPass1!",
                PhoneNumber = "1234567890",
                Gender = "male",
                RoleId = 2
            };
            var expectedUserId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.AdminCreateUserAsync(request, "admin"))
                .ReturnsAsync(expectedUserId);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _userServiceMock.Verify(s => s.AdminCreateUserAsync(request, "admin"), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "New User",
                Email = "newuser@example.com",
                Password = "StrongPass1!",
                PhoneNumber = "1234567890",
                Gender = "male",
                RoleId = 2
            };
            _userServiceMock.Setup(s => s.AdminCreateUserAsync(request, "admin"))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region UpdateUserStatus Tests

        [Fact]
        public async Task UpdateUserStatus_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new UpdateStatusRequest { Status = "inactive" };
            _userServiceMock.Setup(s => s.UpdateUserStatusAsync(userId, request, "admin"))
                .ReturnsAsync(userId);

            // Act
            var result = await _controller.UpdateUserStatus(userId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _userServiceMock.Verify(s => s.UpdateUserStatusAsync(userId, request, "admin"), Times.Once);
        }

        [Fact]
        public async Task UpdateUserStatus_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new UpdateStatusRequest { Status = "inactive" };
            _userServiceMock.Setup(s => s.UpdateUserStatusAsync(userId, request, "admin"))
                .ThrowsAsync(new Exception("User not found"));

            // Act
            var result = await _controller.UpdateUserStatus(userId, request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion
    }
}


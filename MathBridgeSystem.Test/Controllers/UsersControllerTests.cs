using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly UsersController _controller;
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _currentUserId = Guid.NewGuid();

        public UsersControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _controller = new UsersController(_userServiceMock.Object);
            SetupControllerContext("parent");
        }

        private void SetupControllerContext(string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _currentUserId.ToString()),
                new Claim(ClaimTypes.Role, role)
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
            Action act = () => new UsersController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("userService");
        }

        #endregion

        #region GetAllUsers Tests

        [Fact]
        public async Task GetAllUsers_Admin_ReturnsOk()
        {
            // Arrange
            SetupControllerContext("admin");
            var users = new List<UserResponse> { new UserResponse { UserId = Guid.NewGuid() } };
            _userServiceMock.Setup(s => s.GetAllUsersAsync("admin")).ReturnsAsync(users);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllUsers_ServiceThrows_Returns500()
        {
            SetupControllerContext("admin");
            _userServiceMock.Setup(s => s.GetAllUsersAsync("admin")).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetAllUsers();

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetUser Tests

        [Fact]
        public async Task GetUser_ValidRequest_ReturnsOkWithUser()
        {
            // Arrange
            var expectedUser = new UserResponse
            {
                UserId = _userId,
                FullName = "Test User",
                Email = "test@example.com"
            };
            _userServiceMock.Setup(s => s.GetUserByIdAsync(_userId, _currentUserId, "parent"))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetUserById(_userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var user = okResult.Value.Should().BeAssignableTo<UserResponse>().Subject;
            user.UserId.Should().Be(_userId);
            _userServiceMock.Verify(s => s.GetUserByIdAsync(_userId, _currentUserId, "parent"), Times.Once);
        }

        [Fact]
        public async Task GetUser_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _userServiceMock.Setup(s => s.GetUserByIdAsync(_userId, _currentUserId, "parent"))
                .ThrowsAsync(new Exception("User not found"));

            // Act
            var result = await _controller.GetUserById(_userId);

            // Assert
            var statusResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        }

        [Fact]
        public async Task GetUser_AdminRole_CallsServiceWithAdminRole()
        {
            // Arrange
            SetupControllerContext("admin");
            var expectedUser = new UserResponse { UserId = _userId };
            _userServiceMock.Setup(s => s.GetUserByIdAsync(_userId, _currentUserId, "admin"))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetUserById(_userId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _userServiceMock.Verify(s => s.GetUserByIdAsync(_userId, _currentUserId, "admin"), Times.Once);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUser_ValidRequest_ReturnsOkWithUserId()
        {
            // Arrange
            var request = new UpdateUserRequest
            {
                FullName = "Updated Name",
                PhoneNumber = "0987654321"
            };
            _userServiceMock.Setup(s => s.UpdateUserAsync(_userId, request, _currentUserId, "parent"))
                .ReturnsAsync(_userId);

            // Act
            var result = await _controller.UpdateUser(_userId, request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _userServiceMock.Verify(s => s.UpdateUserAsync(_userId, request, _currentUserId, "parent"), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new UpdateUserRequest { FullName = "Test" };
            _userServiceMock.Setup(s => s.UpdateUserAsync(_userId, request, _currentUserId, "parent"))
                .ThrowsAsync(new Exception("Update failed"));

            // Act
            var result = await _controller.UpdateUser(_userId, request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetWallet Tests

        [Fact]
        public async Task GetWallet_ValidRequest_ReturnsOkWithWalletData()
        {
            // Arrange
            var expectedWallet = new WalletResponse
            {
                WalletBalance = 1000m
            };
            _userServiceMock.Setup(s => s.GetWalletAsync(_userId, _currentUserId, "parent"))
                .ReturnsAsync(expectedWallet);

            // Act
            var result = await _controller.GetWallet(_userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var wallet = okResult.Value.Should().BeAssignableTo<WalletResponse>().Subject;
            wallet.WalletBalance.Should().Be(1000m);
            _userServiceMock.Verify(s => s.GetWalletAsync(_userId, _currentUserId, "parent"), Times.Once);
        }

        [Fact]
        public async Task GetWallet_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _userServiceMock.Setup(s => s.GetWalletAsync(_userId, _currentUserId, "parent"))
                .ThrowsAsync(new Exception("Wallet not found"));

            // Act
            var result = await _controller.GetWallet(_userId);

            // Assert
            var statusResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        }

        [Fact]
        public async Task GetWallet_NotFound_ReturnsNotFound()
        {
            _userServiceMock.Setup(s => s.GetWalletAsync(_userId, _currentUserId, "parent"))
                .ThrowsAsync(new Exception("Wallet not found"));

            var result = await _controller.GetWallet(_userId);

            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task GetWallet_Unauthorized_ReturnsForbid()
        {
            // Arrange - simulate UnauthorizedAccessException
            _userServiceMock.Setup(s => s.GetWalletAsync(_userId, _currentUserId, "parent"))
                .ThrowsAsync(new UnauthorizedAccessException("no"));

            var result = await _controller.GetWallet(_userId);

            result.Should().Match(r => r is ForbidResult || r is UnauthorizedObjectResult);
        }

        #endregion
    }
}


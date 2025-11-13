using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<NotificationConnectionManager> _connectionManagerMock;
        private readonly NotificationController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public NotificationControllerTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _connectionManagerMock = new Mock<NotificationConnectionManager>();
            _controller = new NotificationController(
                _notificationServiceMock.Object,
                _connectionManagerMock.Object);
            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
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
        public void Constructor_NullNotificationService_ThrowsArgumentNullException()
        {
            Action act = () => new NotificationController(null!, _connectionManagerMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
        }

        [Fact]
        public void Constructor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Action act = () => new NotificationController(_notificationServiceMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("connectionManager");
        }

        #endregion

        #region GetUnreadCount Tests

        [Fact]
        public async Task GetUnreadCount_ReturnsOkWithCount()
        {
            // Arrange
            var expectedCount = 5;
            _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(_userId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _controller.GetUnreadCount();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedCount);
            _notificationServiceMock.Verify(s => s.GetUnreadCountAsync(_userId), Times.Once);
        }

        [Fact]
        public async Task GetUnreadCount_NoUnreadNotifications_ReturnsZero()
        {
            // Arrange
            _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(_userId))
                .ReturnsAsync(0);

            // Act
            var result = await _controller.GetUnreadCount();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(0);
        }

        #endregion

        #region GetNotifications Tests

        [Fact]
        public async Task GetNotifications_DefaultPagination_ReturnsNotifications()
        {
            // Arrange
            var expectedNotifications = new List<NotificationResponseDto>
            {
                new NotificationResponseDto { NotificationId = Guid.NewGuid(), Title = "Test 1" },
                new NotificationResponseDto { NotificationId = Guid.NewGuid(), Title = "Test 2" }
            };
            _notificationServiceMock.Setup(s => s.GetNotificationsByUserIdAsync(_userId, 1, 10))
                .ReturnsAsync(expectedNotifications);

            // Act
            var result = await _controller.GetNotifications();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notifications = okResult.Value.Should().BeAssignableTo<List<NotificationResponseDto>>().Subject;
            notifications.Should().HaveCount(2);
            _notificationServiceMock.Verify(s => s.GetNotificationsByUserIdAsync(_userId, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetNotifications_CustomPagination_ReturnsNotifications()
        {
            // Arrange
            var expectedNotifications = new List<NotificationResponseDto>
            {
                new NotificationResponseDto { NotificationId = Guid.NewGuid(), Title = "Test" }
            };
            _notificationServiceMock.Setup(s => s.GetNotificationsByUserIdAsync(_userId, 2, 5))
                .ReturnsAsync(expectedNotifications);

            // Act
            var result = await _controller.GetNotifications(2, 5);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notifications = okResult.Value.Should().BeAssignableTo<List<NotificationResponseDto>>().Subject;
            notifications.Should().HaveCount(1);
            _notificationServiceMock.Verify(s => s.GetNotificationsByUserIdAsync(_userId, 2, 5), Times.Once);
        }

        [Fact]
        public async Task GetNotifications_EmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            _notificationServiceMock.Setup(s => s.GetNotificationsByUserIdAsync(_userId, 1, 10))
                .ReturnsAsync(new List<NotificationResponseDto>());

            // Act
            var result = await _controller.GetNotifications();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notifications = okResult.Value.Should().BeAssignableTo<List<NotificationResponseDto>>().Subject;
            notifications.Should().BeEmpty();
        }

        #endregion

        #region GetUnreadNotifications Tests

        [Fact]
        public async Task GetUnreadNotifications_ReturnsUnreadNotifications()
        {
            // Arrange
            var expectedNotifications = new List<NotificationResponseDto>
            {
                new NotificationResponseDto { NotificationId = Guid.NewGuid(), Title = "Unread 1", IsRead = false },
                new NotificationResponseDto { NotificationId = Guid.NewGuid(), Title = "Unread 2", IsRead = false }
            };
            _notificationServiceMock.Setup(s => s.GetUnreadNotificationsByUserIdAsync(_userId))
                .ReturnsAsync(expectedNotifications);

            // Act
            var result = await _controller.GetUnreadNotifications();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notifications = okResult.Value.Should().BeAssignableTo<List<NotificationResponseDto>>().Subject;
            notifications.Should().HaveCount(2);
            notifications.Should().OnlyContain(n => !n.IsRead);
            _notificationServiceMock.Verify(s => s.GetUnreadNotificationsByUserIdAsync(_userId), Times.Once);
        }

        [Fact]
        public async Task GetUnreadNotifications_NoUnread_ReturnsEmptyList()
        {
            // Arrange
            _notificationServiceMock.Setup(s => s.GetUnreadNotificationsByUserIdAsync(_userId))
                .ReturnsAsync(new List<NotificationResponseDto>());

            // Act
            var result = await _controller.GetUnreadNotifications();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notifications = okResult.Value.Should().BeAssignableTo<List<NotificationResponseDto>>().Subject;
            notifications.Should().BeEmpty();
        }

        #endregion

        #region GetNotification Tests

        [Fact]
        public async Task GetNotification_ExistingNotification_ReturnsOkWithNotification()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var expectedNotification = new NotificationResponseDto
            {
                NotificationId = notificationId,
                Title = "Test Notification"
            };
            _notificationServiceMock.Setup(s => s.GetNotificationByIdAsync(notificationId))
                .ReturnsAsync(expectedNotification);

            // Act
            var result = await _controller.GetNotification(notificationId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notification = okResult.Value.Should().BeAssignableTo<NotificationResponseDto>().Subject;
            notification.NotificationId.Should().Be(notificationId);
            _notificationServiceMock.Verify(s => s.GetNotificationByIdAsync(notificationId), Times.Once);
        }

        [Fact]
        public async Task GetNotification_NonExistingNotification_ReturnsNotFound()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            _notificationServiceMock.Setup(s => s.GetNotificationByIdAsync(notificationId))
                .ReturnsAsync((NotificationResponseDto)null!);

            // Act
            var result = await _controller.GetNotification(notificationId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
            _notificationServiceMock.Verify(s => s.GetNotificationByIdAsync(notificationId), Times.Once);
        }

        #endregion

        #region MarkAsRead Tests

        [Fact]
        public async Task MarkAsRead_ValidNotification_ReturnsOk()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            _notificationServiceMock.Setup(s => s.MarkAsReadAsync(notificationId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkAsRead(notificationId);

            // Assert
            result.Should().BeOfType<ActionResult>();
            _notificationServiceMock.Verify(s => s.MarkAsReadAsync(notificationId), Times.Once);
        }

        #endregion
    }
}


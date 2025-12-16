using FluentAssertions;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Services;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MathBridgeSystem.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notificationRepositoryMock;
        private readonly Mock<NotificationConnectionManager> _connectionManagerMock;
        private readonly Mock<IPubSubNotificationProvider> _pubSubProviderMock;
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly NotificationService _notificationService;
        private readonly NotificationService _notificationServiceNoPubSub; // Dùng để test trường hợp PubSub là null

        public NotificationServiceTests()
        {
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _connectionManagerMock = new Mock<NotificationConnectionManager>();
            _pubSubProviderMock = new Mock<IPubSubNotificationProvider>();
            _contractRepositoryMock = new Mock<IContractRepository>();
            _sessionRepositoryMock = new Mock<ISessionRepository>();

            _notificationService = new NotificationService(
                _notificationRepositoryMock.Object,
                _connectionManagerMock.Object,
                _contractRepositoryMock.Object,
                _sessionRepositoryMock.Object,
                _pubSubProviderMock.Object
            );

            _notificationServiceNoPubSub = new NotificationService(
                _notificationRepositoryMock.Object,
                _connectionManagerMock.Object,
                _contractRepositoryMock.Object,
                _sessionRepositoryMock.Object,
                null 
            );
        }

        // Test: Tạo thông báo thành công (với PubSub)
        [Fact]
        public async Task CreateNotificationAsync_WithPubSub_CreatesAndPublishesNotification()
        {
            // Arrange
            var request = new CreateNotificationRequest { Title = "Test", Message = "Msg" };
            Notification capturedNotification = null;

            _notificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Callback<Notification>(n => capturedNotification = n)
                .Returns(Task.CompletedTask);

            _notificationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _pubSubProviderMock.Setup(p => p.PublishNotificationAsync(It.IsAny<NotificationResponseDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _notificationService.CreateNotificationAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test");
            result.Status.Should().Be("Pending"); 

            _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
            _pubSubProviderMock.Verify(p => p.PublishNotificationAsync(It.IsAny<NotificationResponseDto>(), "notifications"), Times.Once);
            _notificationRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Notification>(n => n.Status == "Sent" && n.SentDate.HasValue)), Times.Once);

            capturedNotification.Status.Should().Be("Sent");
        }

        // Test: Tạo thông báo thành công (KHÔNG có PubSub)
        [Fact]
        public async Task CreateNotificationAsync_WithoutPubSub_CreatesWithoutPublishing()
        {
            // Arrange
            var request = new CreateNotificationRequest { Title = "Test", Message = "Msg" };
            _notificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _notificationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

            // Act
            // Sử dụng service đã được khởi tạo với pubSub = null
            var result = await _notificationServiceNoPubSub.CreateNotificationAsync(request);

            // Assert
            result.Should().NotBeNull();
            _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
            _notificationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Once);

            // Quan trọng: Đảm bảo PubSub KHÔNG được gọi
            _pubSubProviderMock.Verify(p => p.PublishNotificationAsync(It.IsAny<NotificationResponseDto>(), It.IsAny<string>()), Times.Never);
        }

        // Test: Ném lỗi nếu Repository.AddAsync thất bại
        [Fact]
        public async Task CreateNotificationAsync_RepositoryAddFails_ThrowsException()
        {
            // Arrange
            var request = new CreateNotificationRequest();
            _notificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).ThrowsAsync(new Exception("DB Error"));

            // Act
            Func<Task> act = () => _notificationService.CreateNotificationAsync(request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB Error");
            // Đảm bảo các bước sau không được gọi
            _pubSubProviderMock.Verify(p => p.PublishNotificationAsync(It.IsAny<NotificationResponseDto>(), It.IsAny<string>()), Times.Never);
            _notificationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
        }

        // Test: Lấy thông báo bằng ID (tìm thấy)
        [Fact]
        public async Task GetNotificationByIdAsync_NotificationFound_ReturnsDto()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var notification = new Notification { NotificationId = notificationId, Title = "Test", Status = "Sent" };
            _notificationRepositoryMock.Setup(r => r.GetByIdAsync(notificationId)).ReturnsAsync(notification);

            // Act
            var result = await _notificationService.GetNotificationByIdAsync(notificationId);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test");
            result.IsRead.Should().BeFalse(); // Test logic MapToDto
        }

        // Test: Lấy thông báo bằng ID (tìm thấy, đã đọc)
        [Fact]
        public async Task GetNotificationByIdAsync_NotificationFoundAndRead_ReturnsDtoWithIsReadTrue()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var notification = new Notification { NotificationId = notificationId, Status = "Read" }; // Đã đọc
            _notificationRepositoryMock.Setup(r => r.GetByIdAsync(notificationId)).ReturnsAsync(notification);

            // Act
            var result = await _notificationService.GetNotificationByIdAsync(notificationId);

            // Assert
            result.IsRead.Should().BeTrue(); // Test logic MapToDto
        }

        // Test: Lấy thông báo bằng ID (không tìm thấy)
        [Fact]
        public async Task GetNotificationByIdAsync_NotificationNotFound_ReturnsNull()
        {
            // Arrange
            _notificationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Notification)null);

            // Act
            var result = await _notificationService.GetNotificationByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        // Test: Lấy thông báo theo UserID (có phân trang)
        [Fact]
        public async Task GetNotificationsByUserIdAsync_WithPagination_CallsRepositoryWithCorrectParams()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notifications = new List<Notification> { new Notification(), new Notification() };
            _notificationRepositoryMock.Setup(r => r.GetPaginatedByUserIdAsync(userId, 3, 30)).ReturnsAsync(notifications);

            // Act
            var result = await _notificationService.GetNotificationsByUserIdAsync(userId, 3, 30);

            // Assert
            result.Should().HaveCount(2);
            _notificationRepositoryMock.Verify(r => r.GetPaginatedByUserIdAsync(userId, 3, 30), Times.Once);
        }

        // Test: Lấy thông báo chưa đọc theo UserID
        [Fact]
        public async Task GetUnreadNotificationsByUserIdAsync_CallsRepository_ReturnsDtoList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notifications = new List<Notification> { new Notification() };
            _notificationRepositoryMock.Setup(r => r.GetUnreadByUserIdAsync(userId)).ReturnsAsync(notifications);

            // Act
            var result = await _notificationService.GetUnreadNotificationsByUserIdAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            _notificationRepositoryMock.Verify(r => r.GetUnreadByUserIdAsync(userId), Times.Once);
        }

        // Test: Lấy số lượng thông báo chưa đọc
        [Fact]
        public async Task GetUnreadCountAsync_CallsRepository_ReturnsCount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _notificationRepositoryMock.Setup(r => r.GetUnreadCountAsync(userId)).ReturnsAsync(5);

            // Act
            var result = await _notificationService.GetUnreadCountAsync(userId);

            // Assert
            result.Should().Be(5);
        }

        // Test: Đánh dấu đã đọc
        [Fact]
        public async Task MarkAsReadAsync_CallsRepository()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            _notificationRepositoryMock.Setup(r => r.MarkAsReadAsync(notificationId)).Returns(Task.CompletedTask);

            // Act
            await _notificationService.MarkAsReadAsync(notificationId);

            // Assert
            _notificationRepositoryMock.Verify(r => r.MarkAsReadAsync(notificationId), Times.Once);
        }

        // Test: Đánh dấu tất cả đã đọc
        [Fact]
        public async Task MarkAllAsReadAsync_CallsRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _notificationRepositoryMock.Setup(r => r.MarkAllAsReadAsync(userId)).Returns(Task.CompletedTask);

            // Act
            await _notificationService.MarkAllAsReadAsync(userId);

            // Assert
            _notificationRepositoryMock.Verify(r => r.MarkAllAsReadAsync(userId), Times.Once);
        }

        // Test: Xóa một thông báo
        [Fact]
        public async Task DeleteNotificationAsync_CallsRepository()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            _notificationRepositoryMock.Setup(r => r.DeleteAsync(notificationId)).Returns(Task.CompletedTask);

            // Act
            await _notificationService.DeleteNotificationAsync(notificationId);

            // Assert
            _notificationRepositoryMock.Verify(r => r.DeleteAsync(notificationId), Times.Once);
        }

        // Test: Xóa tất cả thông báo
        [Fact]
        public async Task DeleteAllNotificationsAsync_CallsRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _notificationRepositoryMock.Setup(r => r.DeleteAllAsync(userId)).Returns(Task.CompletedTask);

            // Act
            await _notificationService.DeleteAllNotificationsAsync(userId);

            // Assert
            _notificationRepositoryMock.Verify(r => r.DeleteAllAsync(userId), Times.Once);
        }

        // Test: Xuất bản lên PubSub (khi có provider)
        [Fact]
        public async Task PublishToPubSubAsync_WithProvider_CallsPublish()
        {
            // Arrange
            var dto = new NotificationResponseDto();
            var topic = "test-topic";

            // Act
            await _notificationService.PublishToPubSubAsync(dto, topic);

            // Assert
            _pubSubProviderMock.Verify(p => p.PublishNotificationAsync(dto, topic), Times.Once);
        }

        // Test: Xuất bản lên PubSub (khi KHÔNG có provider)
        [Fact]
        public async Task PublishToPubSubAsync_WithoutProvider_DoesNotCallPublish()
        {
            // Arrange
            var dto = new NotificationResponseDto();
            var topic = "test-topic";

            // Act
            await _notificationServiceNoPubSub.PublishToPubSubAsync(dto, topic);

            // Assert
            _pubSubProviderMock.Verify(p => p.PublishNotificationAsync(dto, topic), Times.Never);
        }

        [Fact]
        public async Task CreateRescheduleOrRefundNotificationAsync_ValidRequest_CreatesAndPublishes()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var request = new CreateRescheduleOrRefundNotificationRequest { ContractId = contractId, BookingId = bookingId };

            var contract = new Contract { ContractId = contractId, ParentId = parentId };
            var session = new Session { BookingId = bookingId, ContractId = contractId, SessionDate = DateOnly.FromDateTime(DateTime.Now), StartTime = DateTime.Now };

            _contractRepositoryMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);
            _notificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _notificationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _pubSubProviderMock.Setup(p => p.PublishNotificationAsync(It.IsAny<NotificationResponseDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _notificationService.CreateRescheduleOrRefundNotificationAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(parentId);
            result.NotificationType.Should().Be("RescheduleOrRefund");
            
            _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
            _pubSubProviderMock.Verify(p => p.PublishNotificationAsync(It.IsAny<NotificationResponseDto>(), "notifications"), Times.Once);
        }
    }
}
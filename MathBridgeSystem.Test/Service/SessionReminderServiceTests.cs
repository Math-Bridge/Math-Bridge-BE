using FluentAssertions;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.DTOs; // Cần cho SessionReminderDto
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MathBridgeSystem.Tests.Services
{
    public class SessionReminderServiceTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IPubSubNotificationProvider> _pubSubProviderMock;
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly SessionReminderService _service;

        public SessionReminderServiceTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _pubSubProviderMock = new Mock<IPubSubNotificationProvider>();
            _sessionRepositoryMock = new Mock<ISessionRepository>();

            _service = new SessionReminderService(
                _notificationServiceMock.Object,
                _pubSubProviderMock.Object,
                _sessionRepositoryMock.Object
            );
        }


        private Session CreateMockSession(Guid sessionId, Guid parentId, Guid tutorId, string studentName, string tutorName, DateTime startTime)
        {
            return new Session
            {
                BookingId = sessionId,
                ContractId = Guid.NewGuid(),
                TutorId = tutorId,
                StartTime = startTime,
                VideoCallPlatform = "Zoom",
                Tutor = new User { UserId = tutorId, FullName = tutorName },
                Contract = new Contract
                {
                    ParentId = parentId,
                    Parent = new User { UserId = parentId },
                    Child = new Child { FullName = studentName }
                }
            };
        }


        // Test: Ném lỗi nếu INotificationService là null
        [Fact]
        public void Constructor_NullNotificationService_ThrowsArgumentNullException()
        {
            Action act = () => new SessionReminderService(null, _pubSubProviderMock.Object, _sessionRepositoryMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
        }

        // Test: Ném lỗi nếu IPubSubNotificationProvider là null
        [Fact]
        public void Constructor_NullPubSubProvider_ThrowsArgumentNullException()
        {
            Action act = () => new SessionReminderService(_notificationServiceMock.Object, null, _sessionRepositoryMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("pubSubProvider");
        }

        // Test: Ném lỗi nếu ISessionRepository là null
        [Fact]
        public void Constructor_NullSessionRepository_ThrowsArgumentNullException()
        {
            Action act = () => new SessionReminderService(_notificationServiceMock.Object, _pubSubProviderMock.Object, null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("sessionRepository");
        }


        // Test: Lấy session cho nhắc nhở 24 giờ và map DTO chính xác
        [Fact]
        public async Task GetSessionsForReminderAsync_24hr_ReturnsCorrectlyMappedDto()
        {
            // Arrange
            var sessionTime = DateTime.Now.AddHours(24);
            var mockSession = CreateMockSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Student A", "Tutor B", sessionTime);

            _sessionRepositoryMock.Setup(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Session> { mockSession });

            // Act
            var result = await _service.GetSessionsForReminderAsync(24);

            // Assert
            result.Should().HaveCount(1);
            var dto = result.First();
            dto.ReminderType.Should().Be("24hr");
            dto.StudentName.Should().Be("Student A");
            dto.TutorName.Should().Be("Tutor B");
            dto.ParentId.Should().Be(mockSession.Contract.ParentId);
        }

        // Test: Lấy session cho nhắc nhở 1 giờ và map DTO chính xác
        [Fact]
        public async Task GetSessionsForReminderAsync_1hr_ReturnsCorrectlyMappedDto()
        {
            // Arrange
            var sessionTime = DateTime.Now.AddHours(1);
            var mockSession = CreateMockSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Student A", "Tutor B", sessionTime);

            _sessionRepositoryMock.Setup(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Session> { mockSession });

            // Act
            var result = await _service.GetSessionsForReminderAsync(1);

            // Assert
            result.Should().HaveCount(1);
            result.First().ReminderType.Should().Be("1hr");
        }

        // Test: Trả về danh sách rỗng nếu không có session nào
        [Fact]
        public async Task GetSessionsForReminderAsync_NoSessions_ReturnsEmptyList()
        {
            _sessionRepositoryMock.Setup(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Session>());

            var result = await _service.GetSessionsForReminderAsync(24);

            result.Should().BeEmpty();
        }


        // Test: Lấy các session sắp diễn ra
        [Fact]
        public async Task GetUpcomingSessionsAsync_WhenSessionsExist_ReturnsMappedDto()
        {
            // Arrange
            var sessionTime = DateTime.UtcNow.AddHours(2);
            var mockSession = CreateMockSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Student A", "Tutor B", sessionTime);

            _sessionRepositoryMock.Setup(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Session> { mockSession });

            // Act
            var result = await _service.GetUpcomingSessionsAsync(TimeSpan.FromHours(3));

            // Assert
            result.Should().HaveCount(1);
            var dto = result.First();
            dto.ReminderType.Should().Be("upcoming");
            dto.StudentName.Should().Be("Student A");
        }


        // Test: Tạo thông báo cho cả Phụ huynh và Gia sư
        [Fact]
        public async Task CreateReminderNotificationsAsync_ForOneSession_CallsCreateNotificationTwice()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var sessionDto = new SessionReminderDto
            {
                ParentId = parentId,
                TutorId = tutorId,
                TutorName = "Tutor B",
                StudentName = "Student A",
                SessionStartTime = DateTime.Now.AddHours(24)
            };
            var sessions = new List<SessionReminderDto> { sessionDto };

            var createdNotifications = new List<CreateNotificationRequest>();

            _notificationServiceMock.Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
                .Callback<CreateNotificationRequest>(req => createdNotifications.Add(req))
                .ReturnsAsync(new NotificationResponseDto()); 

            // Act
            await _service.CreateReminderNotificationsAsync(sessions, "24hr");

            // Assert
            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()), Times.Exactly(2));

            var parentNotification = createdNotifications.FirstOrDefault(n => n.UserId == parentId);
            parentNotification.Should().NotBeNull();
            parentNotification.Title.Should().Be("Session Tomorrow");
            parentNotification.Message.Should().Contain("Tutor B");
            parentNotification.NotificationType.Should().Be("SessionReminder24hr");

            var tutorNotification = createdNotifications.FirstOrDefault(n => n.UserId == tutorId);
            tutorNotification.Should().NotBeNull();
            tutorNotification.Title.Should().Be("Session Tomorrow");
            tutorNotification.Message.Should().Contain("Student A");
            tutorNotification.NotificationType.Should().Be("SessionReminder24hr");
        }

        // Test: Không làm gì nếu danh sách session rỗng
        [Fact]
        public async Task CreateReminderNotificationsAsync_EmptyList_DoesNotCallCreate()
        {
            // Arrange
            var sessions = new List<SessionReminderDto>(); 

            // Act
            await _service.CreateReminderNotificationsAsync(sessions, "24hr");

            // Assert
            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()), Times.Never);
        }


        // Test: Xuất bản thông báo lên Topic
        [Fact]
        public async Task PublishRemindersToTopic_WithSessions_CallsPublishBatch()
        {
            // Arrange
            var sessions = new List<SessionReminderDto>
            {
                new SessionReminderDto { ParentId = Guid.NewGuid(), ContractId = Guid.NewGuid(), SessionId = Guid.NewGuid() },
                new SessionReminderDto { ParentId = Guid.NewGuid(), ContractId = Guid.NewGuid(), SessionId = Guid.NewGuid() }
            };
            var topic = "test-topic";

            // Act
            await _service.PublishRemindersToTopic(sessions, topic);

            // Assert
            _pubSubProviderMock.Verify(p => p.PublishBatchNotificationsAsync(
                It.Is<List<NotificationResponseDto>>(list => list.Count == 2),
                topic),
                Times.Once);
        }


        // Test: Kiểm tra và gửi (không tìm thấy session nào)
        [Fact]
        public async Task CheckAndSendRemindersAsync_NoSessionsFound_DoesNotCreateNotifications()
        {
            // Arrange
            _sessionRepositoryMock.Setup(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Session>());

            // Act
            await _service.CheckAndSendRemindersAsync();

            // Assert
            _sessionRepositoryMock.Verify(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Exactly(2));
            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()), Times.Never);
        }

        // Test: Kiểm tra và gửi (chỉ tìm thấy 24hr)
        [Fact]
        public async Task CheckAndSendRemindersAsync_Only24hrSessionsFound_CreatesOnly24hrNotifications()
        {
            // Arrange
            var session24hr = CreateMockSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Student 24", "Tutor 24", DateTime.Now.AddHours(24));

            _sessionRepositoryMock.SetupSequence(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Session> { session24hr })
                .ReturnsAsync(new List<Session>()); 

            _notificationServiceMock.Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
                .ReturnsAsync(new NotificationResponseDto());

            // Act
            await _service.CheckAndSendRemindersAsync();

            // Assert
            _sessionRepositoryMock.Verify(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Exactly(2));

            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()), Times.Exactly(2));
            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.Is<CreateNotificationRequest>(r => r.NotificationType == "SessionReminder24hr")), Times.Exactly(2));
            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.Is<CreateNotificationRequest>(r => r.NotificationType == "SessionReminder1hr")), Times.Never);
        }

        // Test: Kiểm tra và gửi (tìm thấy cả 24hr và 1hr)
        [Fact]
        public async Task CheckAndSendRemindersAsync_BothSessionTypesFound_CreatesAllNotifications()
        {
            // Arrange
            var session24hr = CreateMockSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Student 24", "Tutor 24", DateTime.Now.AddHours(24));
            var session1hr = CreateMockSession(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Student 1", "Tutor 1", DateTime.Now.AddHours(1));

            _sessionRepositoryMock.SetupSequence(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Session> { session24hr }) 
                .ReturnsAsync(new List<Session> { session1hr }); 

            _notificationServiceMock.Setup(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
                .ReturnsAsync(new NotificationResponseDto());

            // Act
            await _service.CheckAndSendRemindersAsync();

            // Assert
            _sessionRepositoryMock.Verify(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Exactly(2));

            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()), Times.Exactly(4));
            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.Is<CreateNotificationRequest>(r => r.NotificationType == "SessionReminder24hr")), Times.Exactly(2));
            _notificationServiceMock.Verify(s => s.CreateNotificationAsync(It.Is<CreateNotificationRequest>(r => r.NotificationType == "SessionReminder1hr")), Times.Exactly(2));
        }
    }
}
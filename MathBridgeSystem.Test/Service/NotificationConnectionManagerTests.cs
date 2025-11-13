using FluentAssertions;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Infrastructure.Services;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class NotificationConnectionManagerTests
    {
        private readonly NotificationConnectionManager _manager;
        private readonly NotificationResponseDto _testNotification;

        public NotificationConnectionManagerTests()
        {
            _manager = new NotificationConnectionManager();

            _testNotification = new NotificationResponseDto
            {
                Title = "Test",
                Message = "Hello!",
                IsRead = false,
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };
        }

        private (StreamWriter writer, MemoryStream stream) CreateMockWriter()
        {
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8)
            {
                AutoFlush = false
            };
            return (streamWriter, memoryStream);
        }

        // Đọc nội dung từ MemoryStream
        private string ReadStream(MemoryStream stream)
        {
            stream.Position = 0; 
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        // Test: Đăng ký một kết nối mới
        [Fact]
        public void RegisterConnection_NewUser_AddsConnection()
        {
            var userId = Guid.NewGuid();
            var (writer, stream) = CreateMockWriter();

            _manager.RegisterConnection(userId, writer);

            _manager.GetActiveConnectionCount().Should().Be(1);
            _manager.GetActiveConnectionCountForUser(userId).Should().Be(1);

            stream.Dispose(); 
        }

        // Test: Đăng ký một kết nối đã tồn tại (sẽ thay thế)
        [Fact]
        public void RegisterConnection_ExistingUser_ReplacesWriter()
        {
            var userId = Guid.NewGuid();
            var (writer1, stream1) = CreateMockWriter();
            var (writer2, stream2) = CreateMockWriter();

            _manager.RegisterConnection(userId, writer1);
            _manager.GetActiveConnectionCount().Should().Be(1);

            _manager.RegisterConnection(userId, writer2); 

            _manager.GetActiveConnectionCount().Should().Be(1); 

            stream1.Dispose();
            stream2.Dispose();
        }

        // Test: Hủy đăng ký một kết nối
        [Fact]
        public void UnregisterConnection_ExistingUser_RemovesConnectionAndDisposesWriter()
        {
            var userId = Guid.NewGuid();

            var (writer, stream) = CreateMockWriter();

            _manager.RegisterConnection(userId, writer);
            _manager.GetActiveConnectionCount().Should().Be(1);
            stream.CanWrite.Should().BeTrue();

            _manager.UnregisterConnectionAsync(userId);

            _manager.GetActiveConnectionCount().Should().Be(0);

            stream.CanWrite.Should().BeFalse();
        }

        // Test: Hủy đăng ký một kết nối không tồn tại (không ném lỗi)
        [Fact]
        public void UnregisterConnection_NonExistentUser_DoesNotThrow()
        {
            Action act = () => _manager.UnregisterConnectionAsync(Guid.NewGuid());

            act.Should().NotThrow();
            _manager.GetActiveConnectionCount().Should().Be(0);
        }

        // Test: Gửi tin nhắn cho user đang kết nối
        [Fact]
        public async Task SendNotificationAsync_ConnectedUser_WritesSseMessageToStream()
        {
            var userId = Guid.NewGuid();
            var (writer, stream) = CreateMockWriter();
            _manager.RegisterConnection(userId, writer);

            var expectedJson = JsonSerializer.Serialize(_testNotification);
            var expectedMessage = $"data: {expectedJson}\n\n";

            await _manager.SendNotificationAsync(userId, _testNotification);

            var content = ReadStream(stream);
            content.Should().Be(expectedMessage);

            stream.Dispose(); 
        }

        // Test: Không làm gì khi gửi tin cho user không kết nối
        [Fact]
        public async Task SendNotificationAsync_NonExistentUser_DoesNothing()
        {
            var userId = Guid.NewGuid();

            Func<Task> act = () => _manager.SendNotificationAsync(userId, _testNotification);

            await act.Should().NotThrowAsync();
            _manager.GetActiveConnectionCount().Should().Be(0);
        }

        // Test: Tự động hủy đăng ký nếu StreamWriter bị lỗi khi gửi
        [Fact]
        public async Task SendNotificationAsync_WriterThrowsException_UnregistersConnection()
        {
            var userId = Guid.NewGuid();

            var (writer, stream) = CreateMockWriter();
            _manager.RegisterConnection(userId, writer);
            _manager.GetActiveConnectionCount().Should().Be(1);

            stream.Dispose();

            await _manager.SendNotificationAsync(userId, _testNotification);

            _manager.GetActiveConnectionCount().Should().Be(0);
        }

        // Test: Gửi tin nhắn broadcast cho nhiều user
        [Fact]
        public async Task BroadcastNotificationAsync_AllUsersConnected_SendsToAll()
        {
            var (writer1, stream1) = CreateMockWriter();
            var (writer2, stream2) = CreateMockWriter();
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            _manager.RegisterConnection(userId1, writer1);
            _manager.RegisterConnection(userId2, writer2);

            var expectedJson = JsonSerializer.Serialize(_testNotification);
            var expectedMessage = $"data: {expectedJson}\n\n";

            await _manager.BroadcastNotificationAsync(_testNotification, new[] { userId1, userId2 });

            ReadStream(stream1).Should().Be(expectedMessage);
            ReadStream(stream2).Should().Be(expectedMessage);

            stream1.Dispose();
            stream2.Dispose();
        }

        // Test: Gửi broadcast (1 user lỗi, 1 user thành công)
        [Fact]
        public async Task BroadcastNotificationAsync_OneUserFails_SendsToOthersAndRemovesFailed()
        {
            var (writerSuccess, streamSuccess) = CreateMockWriter();
            var mockWriterFail = new Mock<StreamWriter>(new MemoryStream());
            var userIdSuccess = Guid.NewGuid();
            var userIdFail = Guid.NewGuid();

            mockWriterFail.Setup(w => w.WriteAsync(It.IsAny<string>())).ThrowsAsync(new IOException());

            _manager.RegisterConnection(userIdSuccess, writerSuccess);
            _manager.RegisterConnection(userIdFail, mockWriterFail.Object);

            _manager.GetActiveConnectionCount().Should().Be(2);

            await _manager.BroadcastNotificationAsync(_testNotification, new[] { userIdSuccess, userIdFail });

            ReadStream(streamSuccess).Should().Contain("Hello!");

            _manager.GetActiveConnectionCount().Should().Be(1);
            _manager.GetActiveConnectionCountForUser(userIdFail).Should().Be(0);
            _manager.GetActiveConnectionCountForUser(userIdSuccess).Should().Be(1);

            streamSuccess.Dispose();
            mockWriterFail.Object.Dispose();
        }

        // Test: Gửi broadcast cho danh sách rỗng (không ném lỗi)
        [Fact]
        public async Task BroadcastNotificationAsync_EmptyUserList_CompletesSuccessfully()
        {
            Func<Task> act = () => _manager.BroadcastNotificationAsync(_testNotification, Enumerable.Empty<Guid>());

            await act.Should().NotThrowAsync();
        }

        // Test: Đếm số kết nối active
        [Fact]
        public void GetActiveConnectionCount_ReturnsCorrectCount()
        {
            _manager.GetActiveConnectionCount().Should().Be(0);

            _manager.RegisterConnection(Guid.NewGuid(), CreateMockWriter().writer);
            _manager.RegisterConnection(Guid.NewGuid(), CreateMockWriter().writer);

            _manager.GetActiveConnectionCount().Should().Be(2);
        }

        // Test: Lấy danh sách tất cả user đang kết nối
        [Fact]
        public void GetAllConnectedUsers_ReturnsKeys()
        {
            _manager.GetAllConnectedUsers().Should().BeEmpty();

            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            _manager.RegisterConnection(userId1, CreateMockWriter().writer);
            _manager.RegisterConnection(userId2, CreateMockWriter().writer);

            var users = _manager.GetAllConnectedUsers();

            users.Should().HaveCount(2);
            users.Should().Contain(userId1);
            users.Should().Contain(userId2);
        }
    }
}
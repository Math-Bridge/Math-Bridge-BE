using FluentAssertions;
using MathBridgeSystem.Application.DTOs.VideoConference;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Tests.Helpers; 
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class VideoConferenceServiceTests
    {
        private readonly Mock<MathBridgeDbContext> _contextMock;
        private readonly Mock<IVideoConferenceProvider> _mockProvider;
        private readonly VideoConferenceService _service;

        private readonly Mock<DbSet<Session>> _sessionDbSetMock;
        private readonly Mock<DbSet<Contract>> _contractDbSetMock;
        private readonly Mock<DbSet<VideoConferenceSession>> _videoConferenceDbSetMock;

        private readonly List<Session> _sessionData;
        private readonly List<Contract> _contractData;
        private readonly List<VideoConferenceSession> _videoConferenceData;

        private readonly Guid _bookingId = Guid.NewGuid();
        private readonly Guid _contractId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private const string _platform = "GoogleMeet";

        public VideoConferenceServiceTests()
        {
            _contextMock = new Mock<MathBridgeDbContext>();

            _sessionData = new List<Session>();
            _contractData = new List<Contract>();
            _videoConferenceData = new List<VideoConferenceSession>();

            _sessionDbSetMock = _sessionData.AsQueryable().BuildMockDbSet();
            _contractDbSetMock = _contractData.AsQueryable().BuildMockDbSet();
            _videoConferenceDbSetMock = _videoConferenceData.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(c => c.Sessions).Returns(_sessionDbSetMock.Object);
            _contextMock.Setup(c => c.Contracts).Returns(_contractDbSetMock.Object);
            _contextMock.Setup(c => c.VideoConferenceSessions).Returns(_videoConferenceDbSetMock.Object);

            _sessionDbSetMock.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _sessionData.FirstOrDefault(s => s.BookingId == (Guid)ids[0]));
            _contractDbSetMock.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _contractData.FirstOrDefault(c => c.ContractId == (Guid)ids[0]));

            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockProvider = new Mock<IVideoConferenceProvider>();
            _mockProvider.Setup(p => p.PlatformName).Returns(_platform);

            _service = new VideoConferenceService(
                _contextMock.Object,
                new[] { _mockProvider.Object } 
            );
        }

        #region CreateVideoConferenceAsync Tests

        // Test: Tạo conference thành công (happy path)
        [Fact]
        public async Task CreateVideoConferenceAsync_ValidRequest_CreatesSession()
        {
            // Arrange
            var request = new CreateVideoConferenceRequest { BookingId = _bookingId, ContractId = _contractId, Platform = _platform };
            var creationResult = new VideoConferenceCreationResult
            {
                Success = true,
                MeetingId = "google-id-123",
                MeetingUri = "http://meet.google.com/123",
                MeetingCode = "abc-def-ghi"
            };

            _sessionData.Add(new Session { BookingId = _bookingId });
            _contractData.Add(new Contract { ContractId = _contractId });

            _mockProvider.Setup(p => p.CreateMeetingAsync()).ReturnsAsync(creationResult);

            // Act
            var result = await _service.CreateVideoConferenceAsync(request, _userId);

            // Assert
            result.Should().NotBeNull();
            result.SpaceId.Should().Be("google-id-123");
            result.MeetingUri.Should().Be("http://meet.google.com/123");
            result.Platform.Should().Be(_platform);

            _mockProvider.Verify(p => p.CreateMeetingAsync(), Times.Once);
            _videoConferenceDbSetMock.Verify(db => db.Add(It.Is<VideoConferenceSession>(s => s.BookingId == _bookingId)), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // Test: Ném lỗi khi không tìm thấy Booking
        [Fact]
        public async Task CreateVideoConferenceAsync_BookingNotFound_ThrowsException()
        {
            var request = new CreateVideoConferenceRequest { BookingId = _bookingId };
            _contractData.Add(new Contract { ContractId = _contractId });

            Func<Task> act = () => _service.CreateVideoConferenceAsync(request, _userId);

            await act.Should().ThrowAsync<Exception>();
        }

        // Test: Ném lỗi khi không tìm thấy Contract
        [Fact]
        public async Task CreateVideoConferenceAsync_ContractNotFound_ThrowsException()
        {
            var request = new CreateVideoConferenceRequest { BookingId = _bookingId, ContractId = _contractId };
            _sessionData.Add(new Session { BookingId = _bookingId });

            Func<Task> act = () => _service.CreateVideoConferenceAsync(request, _userId);

            await act.Should().ThrowAsync<Exception>();
        }

        // Test: Ném lỗi khi không tìm thấy Provider (sai platform)
        [Fact]
        public async Task CreateVideoConferenceAsync_ProviderNotFound_ThrowsException()
        {
            var request = new CreateVideoConferenceRequest { BookingId = _bookingId, ContractId = _contractId, Platform = "Zoom" };
            _sessionData.Add(new Session { BookingId = _bookingId });
            _contractData.Add(new Contract { ContractId = _contractId });

            Func<Task> act = () => _service.CreateVideoConferenceAsync(request, _userId);

            await act.Should().ThrowAsync<Exception>();
        }

        // Test: Trả về session đã tồn tại nếu gọi lại (idempotency)
        [Fact]
        public async Task CreateVideoConferenceAsync_MeetingAlreadyExists_ReturnsExistingSession()
        {
            // Arrange
            var request = new CreateVideoConferenceRequest { BookingId = _bookingId, ContractId = _contractId, Platform = _platform };
            var existingSession = new VideoConferenceSession { ConferenceId = Guid.NewGuid(), BookingId = _bookingId, SpaceId = "existing-id" };

            _sessionData.Add(new Session { BookingId = _bookingId });
            _contractData.Add(new Contract { ContractId = _contractId });
            _videoConferenceData.Add(existingSession); 

            // Act
            var result = await _service.CreateVideoConferenceAsync(request, _userId);

            // Assert
            result.Should().NotBeNull();
            result.SpaceId.Should().Be("existing-id"); 

            _mockProvider.Verify(p => p.CreateMeetingAsync(), Times.Never);
            _videoConferenceDbSetMock.Verify(db => db.Add(It.IsAny<VideoConferenceSession>()), Times.Never);
        }

        // Test: Ném lỗi khi Provider tạo meeting thất bại
        [Fact]
        public async Task CreateVideoConferenceAsync_ProviderFails_ThrowsException()
        {
            // Arrange
            var request = new CreateVideoConferenceRequest { BookingId = _bookingId, ContractId = _contractId, Platform = _platform };
            var creationResult = new VideoConferenceCreationResult { Success = false, ErrorMessage = "API Limit Reached" };

            _sessionData.Add(new Session { BookingId = _bookingId });
            _contractData.Add(new Contract { ContractId = _contractId });
            _mockProvider.Setup(p => p.CreateMeetingAsync()).ReturnsAsync(creationResult); 

            // Act
            Func<Task> act = () => _service.CreateVideoConferenceAsync(request, _userId);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        #endregion

        #region GetVideoConferenceAsync Tests

        // Test: Lấy session thành công
        [Fact]
        public async Task GetVideoConferenceAsync_SessionFound_ReturnsDto()
        {
            // Arrange
            var conferenceId = Guid.NewGuid();
            var session = new VideoConferenceSession { ConferenceId = conferenceId, Platform = _platform };
            _videoConferenceData.Add(session);

            // Act
            var result = await _service.GetVideoConferenceAsync(conferenceId);

            // Assert
            result.Should().NotBeNull();
            result.ConferenceId.Should().Be(conferenceId);
            result.Platform.Should().Be(_platform);
        }

        // Test: Ném lỗi khi không tìm thấy session
        [Fact]
        public async Task GetVideoConferenceAsync_SessionNotFound_ThrowsException()
        {
            // Act
            Func<Task> act = () => _service.GetVideoConferenceAsync(Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Video conference session not found");
        }

        #endregion

        #region GetVideoConferencesByBookingAsync Tests

        // Test: Lấy session theo BookingId (tìm thấy)
        [Fact]
        public async Task GetVideoConferencesByBookingAsync_SessionsFound_ReturnsDtoList()
        {
            // Arrange
            _videoConferenceData.Add(new VideoConferenceSession { BookingId = _bookingId, Platform = "A" });
            _videoConferenceData.Add(new VideoConferenceSession { BookingId = _bookingId, Platform = "B" });
            _videoConferenceData.Add(new VideoConferenceSession { BookingId = Guid.NewGuid(), Platform = "C" });

            // Act
            var result = await _service.GetVideoConferencesByBookingAsync(_bookingId);

            // Assert
            result.Should().HaveCount(2);
            result.Select(r => r.Platform).Should().Contain("A").And.Contain("B");
        }

        // Test: Lấy session theo BookingId (không tìm thấy)
        [Fact]
        public async Task GetVideoConferencesByBookingAsync_NoSessions_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetVideoConferencesByBookingAsync(_bookingId);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetVideoConferencesByContractAsync Tests

        // Test: Lấy session theo ContractId (tìm thấy)
        [Fact]
        public async Task GetVideoConferencesByContractAsync_SessionsFound_ReturnsDtoList()
        {
            // Arrange
            _videoConferenceData.Add(new VideoConferenceSession { ContractId = _contractId, Platform = "A" });
            _videoConferenceData.Add(new VideoConferenceSession { ContractId = Guid.NewGuid(), Platform = "B" });
            _videoConferenceData.Add(new VideoConferenceSession { ContractId = _contractId, Platform = "C" });

            // Act
            var result = await _service.GetVideoConferencesByContractAsync(_contractId);

            // Assert
            result.Should().HaveCount(2);
            result.Select(r => r.Platform).Should().Contain("A").And.Contain("C");
        }

        // Test: Lấy session theo ContractId (không tìm thấy)
        [Fact]
        public async Task GetVideoConferencesByContractAsync_NoSessions_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetVideoConferencesByContractAsync(_contractId);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
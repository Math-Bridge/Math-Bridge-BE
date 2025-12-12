using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Tests.Services
{
    public class SessionServiceTests
    {
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IChildRepository> _childRepositoryMock;
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly SessionService _sessionService;

        public SessionServiceTests()
        {
            _sessionRepositoryMock = new Mock<ISessionRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _childRepositoryMock = new Mock<IChildRepository>();
            _contractRepositoryMock = new Mock<IContractRepository>();
            
            _sessionService = new SessionService(
                _sessionRepositoryMock.Object,
                _userRepositoryMock.Object,
                _childRepositoryMock.Object,
                _contractRepositoryMock.Object
            );
        }


        private Session CreateDeepMockSession(
            Guid bookingId,
            Guid parentId,
            Guid tutorId,
            Guid subTutor1Id,
            string sessionStatus = "scheduled")
        {
            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                ParentId = parentId,
                MainTutorId = tutorId,
                SubstituteTutor1Id = subTutor1Id,
                SubstituteTutor2Id = null,
                Parent = new User { UserId = parentId },
                Child = new Child { FullName = "Test Child" },
                Package = new PaymentPackage { PackageName = "Gói 1" }
            };

            return new Session
            {
                BookingId = bookingId,
                ContractId = contract.ContractId,
                Contract = contract,
                TutorId = tutorId,
                Tutor = new User { UserId = tutorId, FullName = "Test Tutor" },
                SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(3)),
                StartTime = DateTime.UtcNow.ToLocalTime().AddDays(3).AddHours(9),
                EndTime = DateTime.UtcNow.ToLocalTime().AddDays(3).AddHours(11),
                Status = sessionStatus,
                IsOnline = true,
                VideoCallPlatform = "Zoom"
            };
        }


        // Test: Lấy session theo ParentId thành công
        [Fact]
        public async Task GetSessionsByParentAsync_WhenSessionsExist_ReturnsMappedDtoList()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var mockSession = CreateDeepMockSession(bookingId, parentId, tutorId, Guid.NewGuid());
            var sessions = new List<Session> { mockSession };

            _sessionRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId)).ReturnsAsync(sessions);

            // Act
            var result = await _sessionService.GetSessionsByParentAsync(parentId);

            // Assert
            result.Should().HaveCount(1);
            var dto = result.First();
            dto.BookingId.Should().Be(bookingId);
            dto.TutorName.Should().Be("Test Tutor");
            dto.ChildName.Should().Be("Test Child");
            dto.PackageName.Should().Be("Gói 1");
        }

        // Test: Lấy session theo ParentId (không có session)
        [Fact]
        public async Task GetSessionsByParentAsync_WhenNoSessions_ReturnsEmptyList()
        {
            // Arrange
            _sessionRepositoryMock.Setup(r => r.GetByParentIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Session>());

            // Act
            var result = await _sessionService.GetSessionsByParentAsync(Guid.NewGuid());

            // Assert
            result.Should().BeEmpty();
        }

        // Test: Lấy session bằng BookingId (thành công và đúng Parent)
        [Fact]
        public async Task GetSessionByIdAsync_SessionFoundAndUserIsOwner_ReturnsDto()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var mockSession = CreateDeepMockSession(bookingId, parentId, Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(mockSession);

            // Act
            var result = await _sessionService.GetSessionByIdAsync(bookingId, parentId);

            // Assert
            result.Should().NotBeNull();
            result.BookingId.Should().Be(bookingId);
        }

        // Test: Lấy session bằng BookingId (không tìm thấy)
        [Fact]
        public async Task GetSessionByIdAsync_SessionNotFound_ReturnsNull()
        {
            // Arrange
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);

            // Act
            var result = await _sessionService.GetSessionByIdAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        // Test: Lấy session bằng BookingId (không phải chủ sở hữu)
        [Fact]
        public async Task GetSessionByIdAsync_UserIsNotOwner_ReturnsNull()
        {
            // Arrange
            var ownerParentId = Guid.NewGuid();
            var otherParentId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var mockSession = CreateDeepMockSession(bookingId, ownerParentId, Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(mockSession);

            // Act
            var result = await _sessionService.GetSessionByIdAsync(bookingId, otherParentId);

            // Assert
            result.Should().BeNull();
        }

        // Test: Lấy session theo ChildId (thành công)
        [Fact]
        public async Task GetSessionsByChildIdAsync_WhenSessionsExist_ReturnsMappedDtoList()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var child = new Child { ChildId = childId, ParentId = parentId };
            var sessions = new List<Session> { CreateDeepMockSession(Guid.NewGuid(), parentId, Guid.NewGuid(), Guid.NewGuid()) };
            
            _childRepositoryMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(child);
            _sessionRepositoryMock.Setup(r => r.GetByChildIdAsync(childId, parentId)).ReturnsAsync(sessions);

            // Act
            var result = await _sessionService.GetSessionsByChildIdAsync(childId);

            // Assert
            result.Should().HaveCount(1);
        }

        // Test: Lấy session theo TutorId (thành công)
        [Fact]
        public async Task GetSessionsByTutorIdAsync_WhenSessionsExist_ReturnsMappedDtoList()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            var sessions = new List<Session> { CreateDeepMockSession(Guid.NewGuid(), Guid.NewGuid(), tutorId, Guid.NewGuid()) };
            _sessionRepositoryMock.Setup(r => r.GetByTutorIdAsync(tutorId)).ReturnsAsync(sessions);

            // Act
            var result = await _sessionService.GetSessionsByTutorIdAsync(tutorId);

            // Assert
            result.Should().HaveCount(1);
        }


        // Test: Cập nhật Tutor thành công
        [Fact]
        public async Task UpdateSessionTutorAsync_ValidChange_UpdatesTutorAndReturnsTrue()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var oldTutorId = Guid.NewGuid();
            var newTutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, parentId, oldTutorId, newTutorId, "scheduled");

            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);
            _sessionRepositoryMock.Setup(r => r.IsTutorAvailableAsync(newTutorId, session.SessionDate, session.StartTime, session.EndTime)).ReturnsAsync(true);

            // Act
            var result = await _sessionService.UpdateSessionTutorAsync(bookingId, newTutorId, Guid.NewGuid());

            // Assert
            result.Should().BeTrue();
            session.TutorId.Should().Be(newTutorId);
            session.UpdatedAt.Should().NotBeNull();
            _sessionRepositoryMock.Verify(r => r.UpdateAsync(session), Times.Once);
        }

        // Test: Ném lỗi khi không tìm thấy Session
        [Fact]
        public async Task UpdateSessionTutorAsync_SessionNotFound_ThrowsKeyNotFoundException()
        {
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);
            Func<Task> act = () => _sessionService.UpdateSessionTutorAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Session not found.");
        }

        // Test: Ném lỗi khi Tutor mới không nằm trong danh sách của Contract
        [Fact]
        public async Task UpdateSessionTutorAsync_TutorNotAssignedToContract_ThrowsArgumentException()
        {
            var bookingId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var oldTutorId = Guid.NewGuid();
            var subTutorId = Guid.NewGuid();
            var invalidTutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, parentId, oldTutorId, subTutorId, "scheduled");

            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionTutorAsync(bookingId, invalidTutorId, Guid.NewGuid());
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*The selected tutor is not assigned to this contract*");
        }

        // Test: Ném lỗi khi Session đã hoàn thành
        [Fact]
        public async Task UpdateSessionTutorAsync_SessionAlreadyCompleted_ThrowsInvalidOperationException()
        {
            var bookingId = Guid.NewGuid();
            var newTutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), newTutorId, "completed");

            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionTutorAsync(bookingId, newTutorId, Guid.NewGuid());
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot update tutor for a session with status 'completed'*");
        }

        // Test: Ném lỗi khi Tutor mới không rảnh
        [Fact]
        public async Task UpdateSessionTutorAsync_NewTutorNotAvailable_ThrowsInvalidOperationException()
        {
            var bookingId = Guid.NewGuid();
            var newTutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), newTutorId, "scheduled");

            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);
            _sessionRepositoryMock.Setup(r => r.IsTutorAvailableAsync(newTutorId, session.SessionDate, session.StartTime, session.EndTime)).ReturnsAsync(false);

            Func<Task> act = () => _sessionService.UpdateSessionTutorAsync(bookingId, newTutorId, Guid.NewGuid());
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*The selected tutor is not available*");
        }


        // Test: Cập nhật status thành công (completed)
        [Fact]
        public async Task UpdateSessionStatusAsync_ValidChangeToCompleted_UpdatesStatusAndReturnsTrue()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "processing");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            // Act
            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "completed", tutorId);

            // Assert
            result.Should().BeTrue();
            session.Status.Should().Be("completed");
            _sessionRepositoryMock.Verify(r => r.UpdateAsync(session), Times.Once);
        }

        // Test: Cập nhật status thành công (cancelled)
        [Fact]
        public async Task UpdateSessionStatusAsync_ValidChangeToCancelled_UpdatesStatusAndReturnsTrue()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "processing");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "cancelled", tutorId);

            result.Should().BeTrue();
            session.Status.Should().Be("cancelled");
        }

        // Test: Ném lỗi khi không tìm thấy Session
        [Fact]
        public async Task UpdateSessionStatusAsync_SessionNotFound_ThrowsKeyNotFoundException()
        {
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);
            Func<Task> act = () => _sessionService.UpdateSessionStatusAsync(Guid.NewGuid(), "completed", Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Session not found.");
        }

        // Test: Ném lỗi khi không đúng Tutor
        [Fact]
        public async Task UpdateSessionStatusAsync_UserNotTutorOfSession_ThrowsUnauthorizedAccessException()
        {
            var bookingId = Guid.NewGuid();
            var sessionTutorId = Guid.NewGuid();
            var otherTutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), sessionTutorId, Guid.NewGuid(), "processing");
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionStatusAsync(bookingId, "completed", otherTutorId);
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You are not the tutor assigned to this session.");
        }

        // Test: Session can be updated even if not in 'processing' state (service doesn't validate this)
        [Fact]
        public async Task UpdateSessionStatusAsync_SessionNotProcessing_UpdatesStatus()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "scheduled");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);
            _sessionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Session>())).Returns(Task.CompletedTask);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "completed", tutorId);
            result.Should().BeTrue();
            _sessionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Session>()), Times.Once);
        }

        // Test: Only allowed statuses are validated
        [Fact]
        public async Task UpdateSessionStatusAsync_AllowedStatus_UpdatesStatus()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "processing");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);
            _sessionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Session>())).Returns(Task.CompletedTask);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "scheduled", tutorId);
            result.Should().BeTrue();
            _sessionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Session>()), Times.Once);
        }


        // Test: Lấy thông tin (rút gọn) cho Tutor
        [Fact]
        public async Task GetSessionForTutorCheckAsync_ValidTutor_ReturnsPartialDto()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "processing");
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionForTutorCheckAsync(bookingId, tutorId);

            result.Should().NotBeNull();
            result.Status.Should().Be("processing");
            result.TutorName.Should().Be("Test Tutor");
            result.ChildName.Should().BeNull();
        }

        // Test: Ném lỗi khi Tutor không đúng
        [Fact]
        public async Task GetSessionForTutorCheckAsync_InvalidTutor_ReturnsNull()
        {
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionForTutorCheckAsync(bookingId, Guid.NewGuid()); // Tutor ID khác

            result.Should().BeNull();
        }


        // Test: Lấy session (vai trò Parent và là chủ)
        [Fact]
        public async Task GetSessionByBookingIdAsync_AsParentOwner_ReturnsDto()
        {
            var parentId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, parentId, Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionByBookingIdAsync(bookingId, parentId, "parent");

            result.Should().NotBeNull();
            result.BookingId.Should().Be(bookingId);
        }

        // Test: Lấy session (vai trò Parent và KHÔNG phải chủ)
        [Fact]
        public async Task GetSessionByBookingIdAsync_AsParentNotOwner_ReturnsNull()
        {
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionByBookingIdAsync(bookingId, Guid.NewGuid(), "parent");

            result.Should().BeNull();
        }

        // Test: Lấy session (vai trò Tutor và là chủ)
        [Fact]
        public async Task GetSessionByBookingIdAsync_AsTutorOwner_ReturnsDto()
        {
            var tutorId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionByBookingIdAsync(bookingId, tutorId, "tutor");

            result.Should().NotBeNull();
        }

        // Test: Lấy session (vai trò Tutor và KHÔNG phải chủ)
        [Fact]
        public async Task GetSessionByBookingIdAsync_AsTutorNotOwner_ReturnsNull()
        {
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionByBookingIdAsync(bookingId, Guid.NewGuid(), "tutor");

            result.Should().BeNull();
        }

        //Test: Lấy session(vai trò Staff)
        [Fact]
        public async Task GetSessionByBookingIdAsync_AsStaff_ReturnsDto()
        {
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionByBookingIdAsync(bookingId, Guid.NewGuid(), "staff");

            result.Should().NotBeNull();
        }

        // ==================== Additional Tests for GetSessionsByChildIdAsync ====================

        [Fact]
        public async Task GetSessionsByChildIdAsync_WhenNoSessions_ReturnsEmptyList()
        {
            var childId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var child = new Child { ChildId = childId, ParentId = parentId };
            
            _childRepositoryMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(child);
            _sessionRepositoryMock.Setup(r => r.GetByChildIdAsync(childId, parentId)).ReturnsAsync(new List<Session>());

            var result = await _sessionService.GetSessionsByChildIdAsync(childId);

            result.Should().BeEmpty();
        }

        // ==================== Tests for UpdateSessionStatusAsync - Today Validation ====================

        [Fact]
        public async Task UpdateSessionStatusAsync_SessionNotToday_ThrowsInvalidOperationException()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "scheduled");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)); // Future date
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionStatusAsync(bookingId, "processing", tutorId);
            
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*You can only update sessions scheduled for today*");
        }

        [Fact]
        public async Task UpdateSessionStatusAsync_InvalidStatus_ThrowsArgumentException()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "scheduled");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionStatusAsync(bookingId, "invalid_status", tutorId);
            
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Invalid status 'invalid_status'*");
        }

        [Fact]
        public async Task UpdateSessionStatusAsync_TryingToChangeFinalStatus_ThrowsInvalidOperationException()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "completed");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionStatusAsync(bookingId, "processing", tutorId);
            
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot change status from 'completed'*");
        }

        [Fact]
        public async Task UpdateSessionStatusAsync_ValidStatusChange_UpdatesSuccessfully()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "scheduled");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "processing", tutorId);

            result.Should().BeTrue();
            session.Status.Should().Be("processing");
            session.UpdatedAt.Should().NotBeNull();
            _sessionRepositoryMock.Verify(r => r.UpdateAsync(session), Times.Once);
        }

        [Fact]
        public async Task UpdateSessionStatusAsync_StatusToRescheduled_UpdatesSuccessfully()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "scheduled");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "rescheduled", tutorId);

            result.Should().BeTrue();
            session.Status.Should().Be("rescheduled");
        }

        // ==================== Tests for GetSessionForTutorCheckAsync ====================

        [Fact]
        public async Task GetSessionForTutorCheckAsync_SessionNotFound_ReturnsNull()
        {
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);

            var result = await _sessionService.GetSessionForTutorCheckAsync(Guid.NewGuid(), Guid.NewGuid());

            result.Should().BeNull();
        }

        // ==================== Tests for GetSessionByIdAsync ====================

        [Fact]
        public async Task GetSessionByIdAsync_ValidOwner_ReturnsDto()
        {
            var parentId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, parentId, Guid.NewGuid(), Guid.NewGuid());
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionByIdAsync(bookingId, parentId);

            result.Should().NotBeNull();
            result.BookingId.Should().Be(bookingId);
            result.ChildName.Should().Be("Test Child");
        }

        // ==================== Tests for GetSessionsByTutorIdAsync ====================

        [Fact]
        public async Task GetSessionsByTutorIdAsync_WhenNoSessions_ReturnsEmptyList()
        {
            _sessionRepositoryMock.Setup(r => r.GetByTutorIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Session>());

            var result = await _sessionService.GetSessionsByTutorIdAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSessionsByTutorIdAsync_WithMultipleSessions_ReturnsAllSessions()
        {
            var tutorId = Guid.NewGuid();
            var sessions = new List<Session>
            {
                CreateDeepMockSession(Guid.NewGuid(), Guid.NewGuid(), tutorId, Guid.NewGuid()),
                CreateDeepMockSession(Guid.NewGuid(), Guid.NewGuid(), tutorId, Guid.NewGuid()),
                CreateDeepMockSession(Guid.NewGuid(), Guid.NewGuid(), tutorId, Guid.NewGuid())
            };
            
            _sessionRepositoryMock.Setup(r => r.GetByTutorIdAsync(tutorId)).ReturnsAsync(sessions);

            var result = await _sessionService.GetSessionsByTutorIdAsync(tutorId);

            result.Should().HaveCount(3);
        }

        // ==================== Tests for UpdateSessionTutorAsync Edge Cases ====================

        [Fact]
        public async Task UpdateSessionTutorAsync_SessionWithNoContract_ThrowsInvalidOperationException()
        {
            var bookingId = Guid.NewGuid();
            var session = new Session
            {
                BookingId = bookingId,
                TutorId = Guid.NewGuid(),
                Status = "scheduled",
                Contract = null
            };
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionTutorAsync(bookingId, Guid.NewGuid(), Guid.NewGuid());
            
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Session contract not found*");
        }

        [Fact]
        public async Task UpdateSessionTutorAsync_CancelledSession_ThrowsInvalidOperationException()
        {
            var bookingId = Guid.NewGuid();
            var newTutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), newTutorId, "cancelled");
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            Func<Task> act = () => _sessionService.UpdateSessionTutorAsync(bookingId, newTutorId, Guid.NewGuid());
            
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot update tutor for a session with status 'cancelled'*");
        }

        [Fact]
        public async Task UpdateSessionTutorAsync_ChangingToSubstituteTutor_UpdatesSuccessfully()
        {
            var bookingId = Guid.NewGuid();
            var mainTutorId = Guid.NewGuid();
            var subTutor1Id = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), mainTutorId, subTutor1Id, "scheduled");
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);
            _sessionRepositoryMock.Setup(r => r.IsTutorAvailableAsync(subTutor1Id, session.SessionDate, session.StartTime, session.EndTime)).ReturnsAsync(true);

            var result = await _sessionService.UpdateSessionTutorAsync(bookingId, subTutor1Id, Guid.NewGuid());

            result.Should().BeTrue();
            session.TutorId.Should().Be(subTutor1Id);
        }

        // ==================== Tests for GetSessionByBookingIdAsync - Additional Role Scenarios ====================

        [Fact]
        public async Task GetSessionByBookingIdAsync_SessionNotFound_ReturnsNull()
        {
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);

            var result = await _sessionService.GetSessionByBookingIdAsync(Guid.NewGuid(), Guid.NewGuid(), "admin");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSessionByBookingIdAsync_AsAdmin_ReturnsDto()
        {
            var bookingId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.GetSessionByBookingIdAsync(bookingId, Guid.NewGuid(), "admin");

            result.Should().NotBeNull();
            result.BookingId.Should().Be(bookingId);
        }

        // ==================== Tests for UpdateSessionStatusAsync - All Valid Statuses ====================

        [Fact]
        public async Task UpdateSessionStatusAsync_ScheduledStatus_UpdatesSuccessfully()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "processing");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "scheduled", tutorId);

            result.Should().BeTrue();
            session.Status.Should().Be("scheduled");
        }

        [Fact]
        public async Task UpdateSessionStatusAsync_CaseInsensitiveStatus_UpdatesSuccessfully()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "scheduled");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "PROCESSING", tutorId);

            result.Should().BeTrue();
            session.Status.Should().Be("processing");
        }

        [Fact]
        public async Task UpdateSessionStatusAsync_SameStatus_StillUpdates()
        {
            var bookingId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var session = CreateDeepMockSession(bookingId, Guid.NewGuid(), tutorId, Guid.NewGuid(), "processing");
            session.SessionDate = DateOnly.FromDateTime(DateTime.Today);
            
            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(bookingId)).ReturnsAsync(session);

            var result = await _sessionService.UpdateSessionStatusAsync(bookingId, "processing", tutorId);

            result.Should().BeTrue();
            _sessionRepositoryMock.Verify(r => r.UpdateAsync(session), Times.Once);
        }

        // ==================== Tests for Multiple Sessions Scenarios ====================

        [Fact]
        public async Task GetSessionsByParentAsync_WithMultipleSessions_ReturnsMappedDtos()
        {
            var parentId = Guid.NewGuid();
            var sessions = new List<Session>
            {
                CreateDeepMockSession(Guid.NewGuid(), parentId, Guid.NewGuid(), Guid.NewGuid(), "scheduled"),
                CreateDeepMockSession(Guid.NewGuid(), parentId, Guid.NewGuid(), Guid.NewGuid(), "processing"),
                CreateDeepMockSession(Guid.NewGuid(), parentId, Guid.NewGuid(), Guid.NewGuid(), "completed")
            };
            
            _sessionRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId)).ReturnsAsync(sessions);

            var result = await _sessionService.GetSessionsByParentAsync(parentId);

            result.Should().HaveCount(3);
            result.Select(s => s.Status).Should().Contain(new[] { "scheduled", "processing", "completed" });
        }

        [Fact]
        public async Task GetSessionsByChildIdAsync_WithMultipleSessions_ReturnsMappedDtos()
        {
            var childId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var child = new Child { ChildId = childId, ParentId = parentId };
            var sessions = new List<Session>
            {
                CreateDeepMockSession(Guid.NewGuid(), parentId, Guid.NewGuid(), Guid.NewGuid()),
                CreateDeepMockSession(Guid.NewGuid(), parentId, Guid.NewGuid(), Guid.NewGuid())
            };
            
            _childRepositoryMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(child);
            _sessionRepositoryMock.Setup(r => r.GetByChildIdAsync(childId, parentId)).ReturnsAsync(sessions);

            var result = await _sessionService.GetSessionsByChildIdAsync(childId);

            result.Should().HaveCount(2);
        }
    }
}
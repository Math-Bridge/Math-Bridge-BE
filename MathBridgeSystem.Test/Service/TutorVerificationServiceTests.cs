using FluentAssertions;
using MathBridgeSystem.Application.DTOs.TutorVerification;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class TutorVerificationServiceTests
    {
        private readonly Mock<ITutorVerificationRepository> _repositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly TutorVerificationService _service;

        private readonly User _mockTutor;
        private readonly Guid _tutorId = Guid.NewGuid();

        public TutorVerificationServiceTests()
        {
            _repositoryMock = new Mock<ITutorVerificationRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();

            _service = new TutorVerificationService(
                _repositoryMock.Object,
                _userRepositoryMock.Object
            );

            _mockTutor = new User
            {
                UserId = _tutorId,
                Role = new Role { RoleName = "tutor" },
                FullName = "Test Tutor",
                Email = "tutor@test.com"
            };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(_tutorId)).ReturnsAsync(_mockTutor);
        }

        #region CreateVerificationAsync Tests

        // Test: Tạo thành công
        [Fact]
        public async Task CreateVerificationAsync_ValidRequest_CreatesAndReturnsId()
        {
            // Arrange
            var request = new CreateTutorVerificationRequest
            {
                UserId = _tutorId,
                University = "Test Uni",
                Major = "Test Major",
                HourlyRate = 100
            };
            var newId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByUserIdAsync(_tutorId)).ReturnsAsync((TutorVerification)null); 
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TutorVerification>()))
                .Callback<TutorVerification>(v => v.VerificationId = newId) 
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateVerificationAsync(request);

            // Assert
            result.Should().Be(newId);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<TutorVerification>(
                v => v.UserId == _tutorId && v.VerificationStatus == "pending" 
            )), Times.Once);
        }

        // Test: Ném lỗi nếu request là null
        [Fact]
        public async Task CreateVerificationAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _service.CreateVerificationAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Ném lỗi nếu thiếu UserId
        [Fact]
        public async Task CreateVerificationAsync_EmptyUserId_ThrowsArgumentException()
        {
            var request = new CreateTutorVerificationRequest { UserId = Guid.Empty };
            Func<Task> act = () => _service.CreateVerificationAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("UserId");
        }

        // Test: Ném lỗi nếu thiếu University
        [Fact]
        public async Task CreateVerificationAsync_MissingUniversity_ThrowsArgumentException()
        {
            var request = new CreateTutorVerificationRequest { UserId = _tutorId, University = " " };
            Func<Task> act = () => _service.CreateVerificationAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("University");
        }

        // Test: Ném lỗi nếu thiếu Major
        [Fact]
        public async Task CreateVerificationAsync_MissingMajor_ThrowsArgumentException()
        {
            var request = new CreateTutorVerificationRequest { UserId = _tutorId, University = "Test Uni", Major = "" };
            Func<Task> act = () => _service.CreateVerificationAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("Major");
        }

        // Test: Ném lỗi nếu HourlyRate <= 0
        [Fact]
        public async Task CreateVerificationAsync_InvalidHourlyRate_ThrowsArgumentException()
        {
            var request = new CreateTutorVerificationRequest { UserId = _tutorId, University = "Test Uni", Major = "Test Major", HourlyRate = 0 };
            Func<Task> act = () => _service.CreateVerificationAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("HourlyRate");
        }

        // Test: Ném lỗi nếu không tìm thấy User
        [Fact]
        public async Task CreateVerificationAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            var request = new CreateTutorVerificationRequest { UserId = Guid.NewGuid(), University = "Test Uni", Major = "Test Major", HourlyRate = 100 };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync((User)null);

            Func<Task> act = () => _service.CreateVerificationAsync(request);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Ném lỗi nếu User không phải là Tutor
        [Fact]
        public async Task CreateVerificationAsync_UserIsNotTutor_ThrowsInvalidOperationException()
        {
            _mockTutor.Role.RoleName = "parent"; 
            var request = new CreateTutorVerificationRequest { UserId = _tutorId, University = "Test Uni", Major = "Test Major", HourlyRate = 100 };

            Func<Task> act = () => _service.CreateVerificationAsync(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*is not a tutor*");
        }

        // Test: Ném lỗi nếu đã tồn tại Verification
        [Fact]
        public async Task CreateVerificationAsync_VerificationAlreadyExists_ThrowsInvalidOperationException()
        {
            var request = new CreateTutorVerificationRequest { UserId = _tutorId, University = "Test Uni", Major = "Test Major", HourlyRate = 100 };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(_tutorId)).ReturnsAsync(new TutorVerification());

            Func<Task> act = () => _service.CreateVerificationAsync(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*Verification already exists*");
        }

        #endregion

        #region UpdateVerificationAsync Tests

        // Test: Cập nhật thành công
        [Fact]
        public async Task UpdateVerificationAsync_ValidRequest_UpdatesVerification()
        {
            // Arrange
            var verificationId = Guid.NewGuid();
            var existing = new TutorVerification { VerificationId = verificationId, University = "Old Uni", HourlyRate = 100 };
            var request = new UpdateTutorVerificationRequest { University = "New Uni", HourlyRate = 150 };

            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(existing);

            // Act
            await _service.UpdateVerificationAsync(verificationId, request);

            // Assert
            existing.University.Should().Be("New Uni");
            existing.HourlyRate.Should().Be(150);
            _repositoryMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        // Test: Ném lỗi khi request là null
        [Fact]
        public async Task UpdateVerificationAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _service.UpdateVerificationAsync(Guid.NewGuid(), null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Ném lỗi khi ID rỗng
        [Fact]
        public async Task UpdateVerificationAsync_EmptyId_ThrowsArgumentException()
        {
            Func<Task> act = () => _service.UpdateVerificationAsync(Guid.Empty, new UpdateTutorVerificationRequest());
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("verificationId");
        }

        // Test: Ném lỗi khi không tìm thấy
        [Fact]
        public async Task UpdateVerificationAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorVerification)null);
            Func<Task> act = () => _service.UpdateVerificationAsync(Guid.NewGuid(), new UpdateTutorVerificationRequest());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Không gọi Update nếu không có gì thay đổi
        [Fact]
        public async Task UpdateVerificationAsync_NoChanges_DoesNotCallUpdate()
        {
            var verificationId = Guid.NewGuid();
            var existing = new TutorVerification { VerificationId = verificationId, University = "Same Uni" };
            var request = new UpdateTutorVerificationRequest { University = "Same Uni", Bio = null };

            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(existing);

            await _service.UpdateVerificationAsync(verificationId, request);

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TutorVerification>()), Times.Never);
        }

        // Test: Không cập nhật HourlyRate nếu <= 0
        [Fact]
        public async Task UpdateVerificationAsync_InvalidHourlyRate_DoesNotUpdateRate()
        {
            var verificationId = Guid.NewGuid();
            var existing = new TutorVerification { VerificationId = verificationId, HourlyRate = 100 };
            var request = new UpdateTutorVerificationRequest { HourlyRate = 0 }; 

            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(existing);

            await _service.UpdateVerificationAsync(verificationId, request);

            existing.HourlyRate.Should().Be(100); 
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TutorVerification>()), Times.Never); 
        }

        #endregion

        #region Get/Query Tests

        // Test: Lấy bằng ID (tìm thấy)
        [Fact]
        public async Task GetVerificationByIdAsync_Found_ReturnsDto()
        {
            var verificationId = Guid.NewGuid();
            var verification = new TutorVerification { VerificationId = verificationId, User = _mockTutor };
            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(verification);

            var result = await _service.GetVerificationByIdAsync(verificationId);

            result.Should().NotBeNull();
            result.VerificationId.Should().Be(verificationId);
            result.UserFullName.Should().Be(_mockTutor.FullName);
        }

        // Test: Lấy bằng ID (không tìm thấy)
        [Fact]
        public async Task GetVerificationByIdAsync_NotFound_ReturnsNull()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorVerification)null);
            var result = await _service.GetVerificationByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // Test: Lấy bằng UserID (tìm thấy)
        [Fact]
        public async Task GetVerificationByUserIdAsync_Found_ReturnsDto()
        {
            var verification = new TutorVerification { UserId = _tutorId, User = _mockTutor };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(_tutorId)).ReturnsAsync(verification);

            var result = await _service.GetVerificationByUserIdAsync(_tutorId);

            result.Should().NotBeNull();
            result.UserId.Should().Be(_tutorId);
        }

        // Test: Lấy tất cả
        [Fact]
        public async Task GetAllVerificationsAsync_ReturnsAll()
        {
            var list = new List<TutorVerification> { new TutorVerification(), new TutorVerification() };
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

            var result = await _service.GetAllVerificationsAsync();

            result.Should().HaveCount(2);
        }

        // Test: Lấy theo Status
        [Fact]
        public async Task GetVerificationsByStatusAsync_CallsRepository()
        {
            // SỬA LỖI: Dùng 'VerificationStatus'
            var list = new List<TutorVerification> { new TutorVerification { VerificationStatus = "pending" } };
            _repositoryMock.Setup(r => r.GetByStatusAsync("pending")).ReturnsAsync(list);

            var result = await _service.GetVerificationsByStatusAsync("pending");

            result.Should().HaveCount(1);
            _repositoryMock.Verify(r => r.GetByStatusAsync("pending"), Times.Once);
        }

        // Test: Ném lỗi khi Get theo Status (status rỗng)
        [Fact]
        public async Task GetVerificationsByStatusAsync_NullStatus_ThrowsArgumentException()
        {
            Func<Task> act = () => _service.GetVerificationsByStatusAsync(" ");
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("status");
        }

        #endregion

        #region Status Change Tests (Approve, Reject, Pending)

        // Test: Duyệt thành công
        [Fact]
        public async Task ApproveVerificationAsync_Valid_UpdatesStatusToApproved()
        {
            var verificationId = Guid.NewGuid();
            var verification = new TutorVerification { VerificationId = verificationId, VerificationStatus = "pending" };
            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(verification);

            await _service.ApproveVerificationAsync(verificationId);

            verification.VerificationStatus.Should().Be("approved");
            verification.VerificationDate.Should().BeCloseTo(DateTime.UtcNow.ToLocalTime(), TimeSpan.FromSeconds(1));
            _repositoryMock.Verify(r => r.UpdateAsync(verification), Times.Once);
        }

        // Test: Ném lỗi khi duyệt (không tìm thấy)
        [Fact]
        public async Task ApproveVerificationAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorVerification)null);
            Func<Task> act = () => _service.ApproveVerificationAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Từ chối thành công
        [Fact]
        public async Task RejectVerificationAsync_Valid_UpdatesStatusToRejected()
        {
            var verificationId = Guid.NewGuid();
            var verification = new TutorVerification { VerificationId = verificationId, VerificationStatus = "pending" };
            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(verification);

            await _service.RejectVerificationAsync(verificationId);

            verification.VerificationStatus.Should().Be("rejected");
            verification.VerificationDate.Should().NotBeNull();
            var diffUtc = (verification.VerificationDate.Value - DateTime.UtcNow).Duration();
            var diffLocal = (verification.VerificationDate.Value - DateTime.UtcNow.ToLocalTime()).Duration();
            (diffUtc <= TimeSpan.FromSeconds(1) || diffLocal <= TimeSpan.FromSeconds(1)).Should().BeTrue("VerificationDate should be within 1s of now in either UTC or local time");
            _repositoryMock.Verify(r => r.UpdateAsync(verification), Times.Once);
        }

        // Test: Chuyển về Pending thành công
        [Fact]
        public async Task PendingVerificationAsync_Valid_UpdatesStatusToPending()
        {
            var verificationId = Guid.NewGuid();
            var verification = new TutorVerification { VerificationId = verificationId, VerificationStatus = "approved" };
            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(verification);

            await _service.PendingVerificationAsync(verificationId);

            verification.VerificationStatus.Should().Be("pending");
            _repositoryMock.Verify(r => r.UpdateAsync(verification), Times.Once);
        }

        #endregion

        #region Delete/Restore Tests

        // Test: Xóa mềm
        [Fact]
        public async Task SoftDeleteVerificationAsync_Valid_CallsRepository()
        {
            var verificationId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(new TutorVerification());

            await _service.SoftDeleteVerificationAsync(verificationId);

            _repositoryMock.Verify(r => r.SoftDeleteAsync(verificationId), Times.Once);
        }

        // Test: Khôi phục
        [Fact]
        public async Task RestoreVerificationAsync_Valid_CallsRepository()
        {
            var verificationId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetDeletedByIdAsync(verificationId)).ReturnsAsync(new TutorVerification());

            await _service.RestoreVerificationAsync(verificationId);

            _repositoryMock.Verify(r => r.RestoreAsync(verificationId), Times.Once);
        }

        // Test: Ném lỗi khi khôi phục (không tìm thấy)
        [Fact]
        public async Task RestoreVerificationAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repositoryMock.Setup(r => r.GetDeletedByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorVerification)null);
            Func<Task> act = () => _service.RestoreVerificationAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Xóa vĩnh viễn
        [Fact]
        public async Task PermanentlyDeleteVerificationAsync_Valid_CallsRepository()
        {
            var verificationId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync(new TutorVerification());

            await _service.PermanentlyDeleteVerificationAsync(verificationId);

            _repositoryMock.Verify(r => r.PermanentDeleteAsync(verificationId), Times.Once);
        }

        // Test: Ném lỗi khi xóa vĩnh viễn (không tìm thấy)
        [Fact]
        public async Task PermanentlyDeleteVerificationAsync_NotFound_ThrowsKeyNotFoundException()
        {
            var verificationId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(verificationId)).ReturnsAsync((TutorVerification)null);
            _repositoryMock.Setup(r => r.GetDeletedByIdAsync(verificationId)).ReturnsAsync((TutorVerification)null);

            Func<Task> act = () => _service.PermanentlyDeleteVerificationAsync(verificationId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        #endregion

        #region Existence Checks Tests

        // Test: Kiểm tra tồn tại (true)
        [Fact]
        public async Task VerificationExistsAsync_Exists_ReturnsTrue()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new TutorVerification());
            var result = await _service.VerificationExistsAsync(Guid.NewGuid());
            result.Should().BeTrue();
        }

        // Test: Kiểm tra tồn tại (false)
        [Fact]
        public async Task VerificationExistsAsync_NotExists_ReturnsFalse()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorVerification)null);
            var result = await _service.VerificationExistsAsync(Guid.NewGuid());
            result.Should().BeFalse();
        }

        // Test: Kiểm tra tồn tại bằng UserId
        [Fact]
        public async Task VerificationExistsByUserIdAsync_Exists_ReturnsTrue()
        {
            _repositoryMock.Setup(r => r.ExistsByUserIdAsync(_tutorId)).ReturnsAsync(true);
            var result = await _service.VerificationExistsByUserIdAsync(_tutorId);
            result.Should().BeTrue();
        }

        #endregion
    }
}
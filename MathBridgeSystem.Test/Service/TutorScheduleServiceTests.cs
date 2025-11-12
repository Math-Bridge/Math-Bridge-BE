using FluentAssertions;
using MathBridgeSystem.Application.DTOs.TutorSchedule;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class TutorScheduleServiceTests
    {
        private readonly Mock<ITutorScheduleRepository> _availabilityRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly TutorScheduleService _service;

        private readonly User _mockTutor;
        private readonly Guid _tutorId = Guid.NewGuid();

        public TutorScheduleServiceTests()
        {
            _availabilityRepositoryMock = new Mock<ITutorScheduleRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();

            _service = new TutorScheduleService(
                _availabilityRepositoryMock.Object,
                _userRepositoryMock.Object
            );

            _mockTutor = new User
            {
                UserId = _tutorId,
                FullName = "Test Tutor",
                Email = "tutor@test.com",
                TutorVerification = new TutorVerification { VerificationStatus = "approved" }
            };

            _userRepositoryMock.Setup(r => r.GetTutorWithVerificationAsync(_tutorId)).ReturnsAsync(_mockTutor);
        }

        private CreateTutorScheduleRequest CreateValidRequest()
        {
            return new CreateTutorScheduleRequest
            {
                TutorId = _tutorId,
                DaysOfWeek = 4, // Tuesday 
                AvailableFrom = new TimeOnly(17, 0, 0), 
                AvailableUntil = new TimeOnly(19, 0, 0), 
                EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
                CanTeachOnline = true,
                CanTeachOffline = true,
                isBooked = false
            };
        }

        #region Constructor Tests

        // Test: Ném lỗi nếu Repository là null
        [Fact]
        public void Constructor_NullAvailabilityRepository_ThrowsArgumentNullException()
        {
            Action act = () => new TutorScheduleService(null, _userRepositoryMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("availabilityRepository");
        }

        // Test: Ném lỗi nếu UserRepository là null
        [Fact]
        public void Constructor_NullUserRepository_ThrowsArgumentNullException()
        {
            Action act = () => new TutorScheduleService(_availabilityRepositoryMock.Object, null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("userRepository");
        }

        #endregion

        #region CreateAvailabilityAsync Tests

        // Test: Tạo lịch thành công
        [Fact]
        public async Task CreateAvailabilityAsync_ValidRequest_CreatesAvailability()
        {
            // Arrange
            var request = CreateValidRequest();
            var newId = Guid.NewGuid();
            _availabilityRepositoryMock.Setup(r => r.GetByTutorIdAsync(_tutorId)).ReturnsAsync(new List<TutorSchedule>());

            _availabilityRepositoryMock.Setup(r => r.HasConflictAsync(
                _tutorId,
                (byte)request.DaysOfWeek, 
                request.AvailableFrom,
                request.AvailableUntil,
                request.EffectiveFrom, 
                request.EffectiveUntil, 
                null)).ReturnsAsync(false);

            _availabilityRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TutorSchedule>()))
                .ReturnsAsync((TutorSchedule ts) => { ts.AvailabilityId = newId; return ts; });

            // Act
            var result = await _service.CreateAvailabilityAsync(request);

            // Assert
            result.Should().Be(newId);
            _availabilityRepositoryMock.Verify(r => r.CreateAsync(It.Is<TutorSchedule>(t => t.TutorId == _tutorId)), Times.Once);
        }


        // Test: Ném lỗi khi DaysOfWeek > 127
        [Fact]
        public async Task CreateAvailabilityAsync_InvalidDaysOfWeek_ThrowsArgumentException()
        {
            var request = CreateValidRequest();
            request.DaysOfWeek = 128; // > 127
            Func<Task> act = () => _service.CreateAvailabilityAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*bitmask must be between 0 and 127*"); 
        }

        // Test: Ném lỗi khi bị trùng lịch (Conflict)
        [Fact]
        public async Task CreateAvailabilityAsync_HasConflict_ThrowsException()
        {
            var request = CreateValidRequest();
            _availabilityRepositoryMock.Setup(r => r.GetByTutorIdAsync(_tutorId)).ReturnsAsync(new List<TutorSchedule>());

            _availabilityRepositoryMock.Setup(r => r.HasConflictAsync(
                It.IsAny<Guid>(),
                It.IsAny<byte>(), 
                It.IsAny<TimeOnly>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<DateOnly>(), 
                It.IsAny<DateOnly?>(), 
                null)).ReturnsAsync(true); 

            Func<Task> act = () => _service.CreateAvailabilityAsync(request);

            await act.Should().ThrowAsync<Exception>().WithMessage("This availability conflicts with an existing time slot");
        }

        #endregion

        #region UpdateAvailabilityAsync Tests

        // Test: Cập nhật thành công
        [Fact]
        public async Task UpdateAvailabilityAsync_ValidRequest_UpdatesAvailability()
        {
            // Arrange
            var availabilityId = Guid.NewGuid();
            var existingSlot = new TutorSchedule
            {
                AvailabilityId = availabilityId,
                TutorId = _tutorId,
                DaysOfWeek = 2, // Monday 
                AvailableFrom = new TimeOnly(17, 0),
                AvailableUntil = new TimeOnly(19, 0),
                EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
                CanTeachOffline = true,
                CanTeachOnline = true,
                IsBooked = false,
                Status = "active"
            };
            var request = new UpdateTutorScheduleRequest
            {
                DaysOfWeek = 4, // Tuesday 
                AvailableFrom = new TimeOnly(18, 0),
                AvailableUntil = new TimeOnly(20, 0),
                CanTeachOnline = false
            };

            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(availabilityId)).ReturnsAsync(existingSlot);

            _availabilityRepositoryMock.Setup(r => r.GetByTutorAndDayAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<TutorSchedule>());
            _availabilityRepositoryMock.Setup(r => r.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly?>(), availabilityId)).ReturnsAsync(false);

            // Act
            await _service.UpdateAvailabilityAsync(availabilityId, request);

            // Assert
            existingSlot.DaysOfWeek.Should().Be(4);
            existingSlot.AvailableFrom.Should().Be(new TimeOnly(18, 0));
            existingSlot.CanTeachOnline.Should().BeFalse();
            _availabilityRepositoryMock.Verify(r => r.UpdateAsync(existingSlot), Times.Once);
        }

        // Test: Ném lỗi khi không tìm thấy lịch
        [Fact]
        public async Task UpdateAvailabilityAsync_NotFound_ThrowsException()
        {
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorSchedule)null);
            Func<Task> act = () => _service.UpdateAvailabilityAsync(Guid.NewGuid(), new UpdateTutorScheduleRequest());
            await act.Should().ThrowAsync<Exception>().WithMessage("Availability not found");
        }


        // Test: Ném lỗi khi cập nhật bị trùng (conflict)
        [Fact]
        public async Task UpdateAvailabilityAsync_HasConflict_ThrowsException()
        {
            var availabilityId = Guid.NewGuid();
            var slot = new TutorSchedule { AvailabilityId = availabilityId, TutorId = _tutorId, DaysOfWeek = 2, AvailableFrom = new TimeOnly(17, 0), AvailableUntil = new TimeOnly(19, 0), EffectiveFrom = DateOnly.FromDateTime(DateTime.Today) };
            var request = new UpdateTutorScheduleRequest { DaysOfWeek = 4 };

            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(availabilityId)).ReturnsAsync(slot);

            _availabilityRepositoryMock.Setup(r => r.GetByTutorAndDayAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly?>())).ReturnsAsync(new List<TutorSchedule>());
            _availabilityRepositoryMock.Setup(r => r.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly?>(), availabilityId)).ReturnsAsync(true); 

            Func<Task> act = () => _service.UpdateAvailabilityAsync(availabilityId, request);

            await act.Should().ThrowAsync<Exception>().WithMessage("Updated availability conflicts with an existing time slot");
        }

        #endregion

        #region DeleteAvailabilityAsync Tests

        // Test: Xóa lịch thành công
        [Fact]
        public async Task DeleteAvailabilityAsync_NotBooked_DeletesSuccessfully()
        {
            var availabilityId = Guid.NewGuid();
            var slot = new TutorSchedule { AvailabilityId = availabilityId, IsBooked = false };
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(availabilityId)).ReturnsAsync(slot);

            await _service.DeleteAvailabilityAsync(availabilityId);

            _availabilityRepositoryMock.Verify(r => r.DeleteAsync(availabilityId), Times.Once);
        }

        // Test: Ném lỗi khi xóa (không tìm thấy)
        [Fact]
        public async Task DeleteAvailabilityAsync_NotFound_ThrowsException()
        {
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorSchedule)null);
            Func<Task> act = () => _service.DeleteAvailabilityAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Availability not found");
        }

        // Test: Ném lỗi khi xóa (lịch đã được đặt)
        [Fact]
        public async Task DeleteAvailabilityAsync_IsBooked_ThrowsException()
        {
            var availabilityId = Guid.NewGuid();
            var slot = new TutorSchedule { AvailabilityId = availabilityId, IsBooked = true }; 
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(availabilityId)).ReturnsAsync(slot);

            Func<Task> act = () => _service.DeleteAvailabilityAsync(availabilityId);

            await act.Should().ThrowAsync<Exception>().WithMessage("Cannot delete availability with active bookings");
        }

        #endregion

        #region Get/Search Tests

        // Test: Lấy lịch bằng ID (tìm thấy)
        [Fact]
        public async Task GetAvailabilityByIdAsync_Found_ReturnsDto()
        {
            var availabilityId = Guid.NewGuid();
            var slot = new TutorSchedule
            {
                AvailabilityId = availabilityId,
                TutorId = _tutorId,
                Tutor = _mockTutor,
                DaysOfWeek = 4 
            };
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(availabilityId)).ReturnsAsync(slot);

            var result = await _service.GetAvailabilityByIdAsync(availabilityId);

            result.Should().NotBeNull();
            result.AvailabilityId.Should().Be(availabilityId);
            result.TutorName.Should().Be("Test Tutor");
            result.DaysOfWeeksDisplay.Should().Be("Tuesday"); 
        }

        // Test: Lấy lịch bằng ID (không tìm thấy)
        [Fact]
        public async Task GetAvailabilityByIdAsync_NotFound_ReturnsNull()
        {
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorSchedule)null);
            var result = await _service.GetAvailabilityByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // Test: Lấy lịch của Tutor (chỉ active)
        [Fact]
        public async Task GetTutorSchedulesAsync_ActiveOnly_CallsCorrectRepoMethod()
        {
            _availabilityRepositoryMock.Setup(r => r.GetActiveTutorSchedulesAsync(_tutorId)).ReturnsAsync(new List<TutorSchedule>());
            await _service.GetTutorSchedulesAsync(_tutorId, true);
            _availabilityRepositoryMock.Verify(r => r.GetActiveTutorSchedulesAsync(_tutorId), Times.Once);
            _availabilityRepositoryMock.Verify(r => r.GetByTutorIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        // Test: Lấy lịch của Tutor (tất cả)
        [Fact]
        public async Task GetTutorSchedulesAsync_All_CallsCorrectRepoMethod()
        {
            _availabilityRepositoryMock.Setup(r => r.GetByTutorIdAsync(_tutorId)).ReturnsAsync(new List<TutorSchedule>());
            await _service.GetTutorSchedulesAsync(_tutorId, false);
            _availabilityRepositoryMock.Verify(r => r.GetActiveTutorSchedulesAsync(It.IsAny<Guid>()), Times.Never);
            _availabilityRepositoryMock.Verify(r => r.GetByTutorIdAsync(_tutorId), Times.Once);
        }

        // Test: Tìm kiếm Tutor (thành công, gộp và phân trang)
        [Fact]
        public async Task SearchAvailableTutorsAsync_ValidRequest_ReturnsGroupedAndPagedResults()
        {
            // Arrange
            var tutorAId = Guid.NewGuid();
            var tutorBId = Guid.NewGuid();
            var tutorA = new User { UserId = tutorAId, FullName = "Tutor A", Email = "a@t.com" };
            var tutorB = new User { UserId = tutorBId, FullName = "Tutor B", Email = "b@t.com" };

            var availabilities = new List<TutorSchedule>
            {
                new TutorSchedule { TutorId = tutorAId, Tutor = tutorA, AvailableFrom = new TimeOnly(10, 0), CanTeachOnline = true },
                new TutorSchedule { TutorId = tutorAId, Tutor = tutorA, AvailableFrom = new TimeOnly(14, 0), CanTeachOnline = true },
                new TutorSchedule { TutorId = tutorBId, Tutor = tutorB, AvailableFrom = new TimeOnly(11, 0), CanTeachOnline = true }
            };

            _availabilityRepositoryMock.Setup(r => r.SearchAvailableTutorsAsync(
                It.IsAny<byte?>(), 
                It.IsAny<TimeOnly?>(),
                It.IsAny<TimeOnly?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateTime?>())) 
                .ReturnsAsync(availabilities);

            var request = new SearchAvailableTutorsRequest { Page = 1, PageSize = 1 }; 

            // Act
            var result = await _service.SearchAvailableTutorsAsync(request);

            // Assert
            result.Should().HaveCount(1); 
            result[0].TutorId.Should().Be(tutorAId); 
            result[0].TutorName.Should().Be("Tutor A");
            result[0].TotalAvailableSlots.Should().Be(2); 
        }

        // Test: Ném lỗi khi tìm kiếm (thời gian không hợp lệ)
        [Fact]
        public async Task SearchAvailableTutorsAsync_InvalidTime_ThrowsArgumentException()
        {
            var request = new SearchAvailableTutorsRequest { StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(10, 0) };
            Func<Task> act = () => _service.SearchAvailableTutorsAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("End time must be after start time");
        }

        #endregion

        #region UpdateAvailabilityStatusAsync Tests

        // Test: Cập nhật status thành công
        [Fact]
        public async Task UpdateAvailabilityStatusAsync_ValidStatus_UpdatesStatus()
        {
            var slot = new TutorSchedule { Status = "active" };
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(slot);

            await _service.UpdateAvailabilityStatusAsync(Guid.NewGuid(), "inactive");

            slot.Status.Should().Be("inactive");
            _availabilityRepositoryMock.Verify(r => r.UpdateAsync(slot), Times.Once);
        }

        // Test: Ném lỗi khi không tìm thấy
        [Fact]
        public async Task UpdateAvailabilityStatusAsync_NotFound_ThrowsException()
        {
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TutorSchedule)null);
            Func<Task> act = () => _service.UpdateAvailabilityStatusAsync(Guid.NewGuid(), "active");
            await act.Should().ThrowAsync<Exception>().WithMessage("Availability not found");
        }

        // Test: Ném lỗi khi status không hợp lệ
        [Fact]
        public async Task UpdateAvailabilityStatusAsync_InvalidStatus_ThrowsArgumentException()
        {
            _availabilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new TutorSchedule());
            Func<Task> act = () => _service.UpdateAvailabilityStatusAsync(Guid.NewGuid(), "pending"); 
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid status*");
        }

        #endregion

        #region BulkCreateAvailabilitiesAsync Tests

        // Test: Tạo hàng loạt thành công
        [Fact]
        public async Task BulkCreateAvailabilitiesAsync_AllValid_CreatesAll()
        {
            // Arrange
            var request1 = CreateValidRequest();
            request1.DaysOfWeek = 2; // Mon
            var request2 = CreateValidRequest();
            request2.DaysOfWeek = 4; // Tue

            _availabilityRepositoryMock.Setup(r => r.GetByTutorIdAsync(_tutorId)).ReturnsAsync(new List<TutorSchedule>());

            _availabilityRepositoryMock.Setup(r => r.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly?>(), null))
                .ReturnsAsync(false);

            _availabilityRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TutorSchedule>()))
                .ReturnsAsync((TutorSchedule ts) => { ts.AvailabilityId = Guid.NewGuid(); return ts; });

            // Act
            var result = await _service.BulkCreateAvailabilitiesAsync(new List<CreateTutorScheduleRequest> { request1, request2 });

            // Assert
            result.Should().HaveCount(2);
            _availabilityRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<TutorSchedule>()), Times.Exactly(2));
        }

        // Test: Ném lỗi khi một request bị lỗi
        [Fact]
        public async Task BulkCreateAvailabilitiesAsync_OneInvalid_ThrowsAndStops()
        {
            // Arrange
            var request1 = CreateValidRequest();
            request1.DaysOfWeek = 2; // Mon
            var request2_Invalid = CreateValidRequest();
            request2_Invalid.DaysOfWeek = 4; // Tue
            request2_Invalid.CanTeachOffline = false;
            request2_Invalid.CanTeachOnline = false; 
            var request3 = CreateValidRequest();
            request3.DaysOfWeek = 8; // Wed

            _availabilityRepositoryMock.Setup(r => r.GetByTutorIdAsync(_tutorId)).ReturnsAsync(new List<TutorSchedule>());

            _availabilityRepositoryMock.Setup(r => r.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly?>(), null))
                .ReturnsAsync(false);

            _availabilityRepositoryMock.Setup(r => r.CreateAsync(It.Is<TutorSchedule>(t => t.DaysOfWeek == 2)))
                .ReturnsAsync((TutorSchedule ts) => { ts.AvailabilityId = Guid.NewGuid(); return ts; });

            // Act
            Func<Task> act = () => _service.BulkCreateAvailabilitiesAsync(new List<CreateTutorScheduleRequest> { request1, request2_Invalid, request3 });

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("*Failed to create availability for day 4*");

            _availabilityRepositoryMock.Verify(r => r.CreateAsync(It.Is<TutorSchedule>(t => t.DaysOfWeek == 2)), Times.Once);
            _availabilityRepositoryMock.Verify(r => r.CreateAsync(It.Is<TutorSchedule>(t => t.DaysOfWeek == 4)), Times.Never);
            _availabilityRepositoryMock.Verify(r => r.CreateAsync(It.Is<TutorSchedule>(t => t.DaysOfWeek == 8)), Times.Never);
        }

        #endregion
    }
}
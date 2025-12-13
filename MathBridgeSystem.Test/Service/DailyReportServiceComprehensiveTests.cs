//using FluentAssertions;
//using MathBridgeSystem.Application.DTOs.DailyReport;
//using MathBridgeSystem.Application.Services;
//using MathBridgeSystem.Domain.Entities;
//using MathBridgeSystem.Domain.Interfaces;
//using Moq;
//using Xunit;

//namespace MathBridgeSystem.Tests.Services
//{
//    public class DailyReportServiceComprehensiveTests
//    {
//        private readonly Mock<IDailyReportRepository> _dailyReportRepositoryMock;
//        private readonly Mock<IUnitRepository> _unitRepositoryMock;
//        private readonly Mock<IPackageRepository> _packageRepositoryMock;
//        private readonly Mock<IContractRepository> _contractRepositoryMock;
//        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
//        private readonly DailyReportService _dailyReportService;

//        public DailyReportServiceComprehensiveTests()
//        {
//            _dailyReportRepositoryMock = new Mock<IDailyReportRepository>();
//            _unitRepositoryMock = new Mock<IUnitRepository>();
//            _packageRepositoryMock = new Mock<IPackageRepository>();
//            _contractRepositoryMock = new Mock<IContractRepository>();
//            _sessionRepositoryMock = new Mock<ISessionRepository>();
//            _dailyReportService = new DailyReportService(
//                _dailyReportRepositoryMock.Object,
//                _unitRepositoryMock.Object,
//                _packageRepositoryMock.Object,
//                _contractRepositoryMock.Object,
//                _sessionRepositoryMock.Object);
//        }

//        #region Constructor Tests

//        [Fact]
//        public void Constructor_NullDailyReportRepository_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            Action act = () => new DailyReportService(null!, _unitRepositoryMock.Object, _packageRepositoryMock.Object, _contractRepositoryMock.Object, _sessionRepositoryMock.Object);
//            act.Should().Throw<ArgumentNullException>();
//        }

//        [Fact]
//        public void Constructor_NullUnitRepository_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            Action act = () => new DailyReportService(_dailyReportRepositoryMock.Object, null!, _packageRepositoryMock.Object, _contractRepositoryMock.Object, _sessionRepositoryMock.Object);
//            act.Should().Throw<ArgumentNullException>();
//        }

//        [Fact]
//        public void Constructor_NullPackageRepository_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            Action act = () => new DailyReportService(_dailyReportRepositoryMock.Object, _unitRepositoryMock.Object, null!, _contractRepositoryMock.Object, _sessionRepositoryMock.Object);
//            act.Should().Throw<ArgumentNullException>();
//        }

//        [Fact]
//        public void Constructor_NullContractRepository_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            Action act = () => new DailyReportService(_dailyReportRepositoryMock.Object, _unitRepositoryMock.Object, _packageRepositoryMock.Object, null!, _sessionRepositoryMock.Object);
//            act.Should().Throw<ArgumentNullException>();
//        }

//        [Fact]
//        public void Constructor_NullSessionRepository_ThrowsArgumentNullException()
//        {
//            // Act & Assert
//            Action act = () => new DailyReportService(_dailyReportRepositoryMock.Object, _unitRepositoryMock.Object, _packageRepositoryMock.Object, _contractRepositoryMock.Object, null!);
//            act.Should().Throw<ArgumentNullException>();
//        }

//        #endregion

//        #region GetDailyReportByIdAsync Tests

//        [Fact]
//        public async Task GetDailyReportByIdAsync_ExistingReport_ReturnsDto()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var report = new DailyReport
//            {
//                ReportId = reportId,
//                ChildId = Guid.NewGuid(),
//                TutorId = Guid.NewGuid(),
//                Notes = "Test notes",
//                OnTrack = true,
//                HaveHomework = false
//            };

//            _dailyReportRepositoryMock.Setup(r => r.GetByIdAsync(reportId)).ReturnsAsync(report);

//            // Act
//            var result = await _dailyReportService.GetDailyReportByIdAsync(reportId);

//            // Assert
//            result.Should().NotBeNull();
//            result.ReportId.Should().Be(reportId);
//            result.Notes.Should().Be("Test notes");
//        }

//        [Fact]
//        public async Task GetDailyReportByIdAsync_NonExistingReport_ThrowsKeyNotFoundException()
//        {
//            // Arrange
//            _dailyReportRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((DailyReport)null!);

//            // Act
//            Func<Task> act = async () => await _dailyReportService.GetDailyReportByIdAsync(Guid.NewGuid());

//            // Assert
//            await act.Should().ThrowAsync<KeyNotFoundException>()
//                .WithMessage("*not found*");
//        }

//        #endregion

//        #region GetDailyReportsByTutorIdAsync Tests

//        [Fact]
//        public async Task GetDailyReportsByTutorIdAsync_ReturnsReports()
//        {
//            // Arrange
//            var tutorId = Guid.NewGuid();
//            var reports = new List<DailyReport>
//            {
//                new DailyReport { ReportId = Guid.NewGuid(), TutorId = tutorId },
//                new DailyReport { ReportId = Guid.NewGuid(), TutorId = tutorId }
//            };

//            _dailyReportRepositoryMock.Setup(r => r.GetByTutorIdAsync(tutorId)).ReturnsAsync(reports);

//            // Act
//            var result = await _dailyReportService.GetDailyReportsByTutorIdAsync(tutorId);

//            // Assert
//            result.Should().HaveCount(2);
//        }

//        [Fact]
//        public async Task GetDailyReportsByTutorIdAsync_NoReports_ReturnsEmptyList()
//        {
//            // Arrange
//            var tutorId = Guid.NewGuid();
//            _dailyReportRepositoryMock.Setup(r => r.GetByTutorIdAsync(tutorId)).ReturnsAsync(new List<DailyReport>());

//            // Act
//            var result = await _dailyReportService.GetDailyReportsByTutorIdAsync(tutorId);

//            // Assert
//            result.Should().BeEmpty();
//        }

//        #endregion

//        #region GetDailyReportsByChildIdAsync Tests

//        [Fact]
//        public async Task GetDailyReportsByChildIdAsync_ReturnsReports()
//        {
//            // Arrange
//            var childId = Guid.NewGuid();
//            var reports = new List<DailyReport>
//            {
//                new DailyReport { ReportId = Guid.NewGuid(), ChildId = childId },
//                new DailyReport { ReportId = Guid.NewGuid(), ChildId = childId }
//            };

//            _dailyReportRepositoryMock.Setup(r => r.GetByChildIdAsync(childId)).ReturnsAsync(reports);

//            // Act
//            var result = await _dailyReportService.GetDailyReportsByChildIdAsync(childId);

//            // Assert
//            result.Should().HaveCount(2);
//        }

//        #endregion

//        #region GetDailyReportsByBookingIdAsync Tests

//        [Fact]
//        public async Task GetDailyReportsByBookingIdAsync_ReturnsReports()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            var reports = new List<DailyReport>
//            {
//                new DailyReport { ReportId = Guid.NewGuid(), BookingId = bookingId }
//            };

//            _dailyReportRepositoryMock.Setup(r => r.GetByBookingIdAsync(bookingId)).ReturnsAsync(reports);

//            // Act
//            var result = await _dailyReportService.GetDailyReportsByBookingIdAsync(bookingId);

//            // Assert
//            result.Should().HaveCount(1);
//        }

//        #endregion

//        #region CreateDailyReportAsync Tests

//        [Fact]
//        public async Task CreateDailyReportAsync_ValidRequest_CreatesReport()
//        {
//            // Arrange
//            var tutorId = Guid.NewGuid();
//            var request = new CreateDailyReportRequest
//            {
//                ChildId = Guid.NewGuid(),
//                BookingId = Guid.NewGuid(),
//                Notes = "Test notes",
//                OnTrack = true,
//                HaveHomework = true,
//                UnitId = Guid.NewGuid()
//            };

//            var createdReport = new DailyReport { ReportId = Guid.NewGuid() };
//            _dailyReportRepositoryMock.Setup(r => r.AddAsync(It.IsAny<DailyReport>()))
//                .ReturnsAsync(createdReport);

//            // Act
//            var result = await _dailyReportService.CreateDailyReportAsync(request, tutorId);

//            // Assert
//            result.Should().NotBeEmpty();
//            _dailyReportRepositoryMock.Verify(r => r.AddAsync(It.Is<DailyReport>(d =>
//                d.ChildId == request.ChildId &&
//                d.TutorId == tutorId &&
//                d.Notes == request.Notes
//            )), Times.Once);
//        }

//        #endregion

//        #region UpdateDailyReportAsync Tests

//        [Fact]
//        public async Task UpdateDailyReportAsync_ValidRequest_UpdatesReport()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var existingReport = new DailyReport
//            {
//                ReportId = reportId,
//                Notes = "Old notes",
//                OnTrack = false,
//                HaveHomework = false
//            };

//            var request = new UpdateDailyReportRequest
//            {
//                Notes = "New notes",
//                OnTrack = true,
//                HaveHomework = true,
//                UnitId = Guid.NewGuid()
//            };

//            _dailyReportRepositoryMock.Setup(r => r.GetByIdAsync(reportId)).ReturnsAsync(existingReport);
//            _dailyReportRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DailyReport>())).ReturnsAsync(new DailyReport());

//            // Act
//            await _dailyReportService.UpdateDailyReportAsync(reportId, request);

//            // Assert
//            existingReport.Notes.Should().Be("New notes");
//            existingReport.OnTrack.Should().BeTrue();
//            existingReport.HaveHomework.Should().BeTrue();
//            _dailyReportRepositoryMock.Verify(r => r.UpdateAsync(existingReport), Times.Once);
//        }

//        [Fact]
//        public async Task UpdateDailyReportAsync_ReportNotFound_ThrowsKeyNotFoundException()
//        {
//            // Arrange
//            var request = new UpdateDailyReportRequest { Notes = "Test" };
//            _dailyReportRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((DailyReport)null!);

//            // Act
//            Func<Task> act = async () => await _dailyReportService.UpdateDailyReportAsync(Guid.NewGuid(), request);

//            // Assert
//            await act.Should().ThrowAsync<KeyNotFoundException>()
//                .WithMessage("*not found*");
//        }

//        [Fact]
//        public async Task UpdateDailyReportAsync_EmptyNotes_DoesNotUpdateNotes()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var existingReport = new DailyReport
//            {
//                ReportId = reportId,
//                Notes = "Original notes"
//            };

//            var request = new UpdateDailyReportRequest
//            {
//                Notes = "",
//                OnTrack = true
//            };

//            _dailyReportRepositoryMock.Setup(r => r.GetByIdAsync(reportId)).ReturnsAsync(existingReport);
//            _dailyReportRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DailyReport>())).ReturnsAsync(new DailyReport());

//            // Act
//            await _dailyReportService.UpdateDailyReportAsync(reportId, request);

//            // Assert
//            existingReport.Notes.Should().Be("Original notes");
//        }

//        #endregion

//        #region DeleteDailyReportAsync Tests

//        [Fact]
//        public async Task DeleteDailyReportAsync_ValidId_ReturnsTrue()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _dailyReportRepositoryMock.Setup(r => r.DeleteAsync(reportId)).ReturnsAsync(true);

//            // Act
//            var result = await _dailyReportService.DeleteDailyReportAsync(reportId);

//            // Assert
//            result.Should().BeTrue();
//            _dailyReportRepositoryMock.Verify(r => r.DeleteAsync(reportId), Times.Once);
//        }

//        [Fact]
//        public async Task DeleteDailyReportAsync_NonExistingReport_ReturnsFalse()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _dailyReportRepositoryMock.Setup(r => r.DeleteAsync(reportId)).ReturnsAsync(false);

//            // Act
//            var result = await _dailyReportService.DeleteDailyReportAsync(reportId);

//            // Assert
//            result.Should().BeFalse();
//        }

//        #endregion
//    }
//}

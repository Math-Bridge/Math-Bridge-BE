using FluentAssertions;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class DailyReportServiceTests
    {
        private readonly Mock<IDailyReportRepository> _dailyReportRepositoryMock;
        private readonly Mock<IUnitRepository> _unitRepositoryMock;
        private readonly Mock<IPackageRepository> _packageRepositoryMock;
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly DailyReportService _service;

        public DailyReportServiceTests()
        {
            _dailyReportRepositoryMock = new Mock<IDailyReportRepository>();
            _unitRepositoryMock = new Mock<IUnitRepository>();
            _packageRepositoryMock = new Mock<IPackageRepository>();
            _contractRepositoryMock = new Mock<IContractRepository>();
            _sessionRepositoryMock = new Mock<ISessionRepository>();

            _service = new DailyReportService(
                _dailyReportRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _contractRepositoryMock.Object,
                _sessionRepositoryMock.Object
            );
        }

        [Fact]
        public async Task GetDailyReportByIdAsync_ShouldReturnReport_WhenExists()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var dailyReport = new DailyReport
            {
                ReportId = reportId,
                TutorId = Guid.NewGuid(),
                ChildId = Guid.NewGuid(),
                Notes = "Great progress today"
            };

            _dailyReportRepositoryMock.Setup(r => r.GetByIdAsync(reportId))
                .ReturnsAsync(dailyReport);

            // Act
            var result = await _service.GetDailyReportByIdAsync(reportId);

            // Assert
            result.Should().NotBeNull();
            result.ReportId.Should().Be(reportId);
        }

        [Fact]
        public async Task GetDailyReportByIdAsync_ShouldThrowKeyNotFoundException_WhenNotExists()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            _dailyReportRepositoryMock.Setup(r => r.GetByIdAsync(reportId))
                .ReturnsAsync((DailyReport)null!);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.GetDailyReportByIdAsync(reportId)
            );
        }

        [Fact]
        public async Task GetDailyReportsByTutorIdAsync_ShouldReturnAllTutorReports()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            var reports = new List<DailyReport>
            {
                new DailyReport { ReportId = Guid.NewGuid(), TutorId = tutorId },
                new DailyReport { ReportId = Guid.NewGuid(), TutorId = tutorId }
            };

            _dailyReportRepositoryMock.Setup(r => r.GetByTutorIdAsync(tutorId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetDailyReportsByTutorIdAsync(tutorId);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetDailyReportsByChildIdAsync_ShouldReturnAllChildReports()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var reports = new List<DailyReport>
            {
                new DailyReport { ReportId = Guid.NewGuid(), ChildId = childId },
                new DailyReport { ReportId = Guid.NewGuid(), ChildId = childId }
            };

            _dailyReportRepositoryMock.Setup(r => r.GetByChildIdAsync(childId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetDailyReportsByChildIdAsync(childId);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetDailyReportsByBookingIdAsync_ShouldReturnAllBookingReports()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var reports = new List<DailyReport>
            {
                new DailyReport { ReportId = Guid.NewGuid(), BookingId = bookingId }
            };

            _dailyReportRepositoryMock.Setup(r => r.GetByBookingIdAsync(bookingId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetDailyReportsByBookingIdAsync(bookingId);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenDailyReportRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new DailyReportService(
                null!,
                _unitRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _contractRepositoryMock.Object,
                _sessionRepositoryMock.Object
            );

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenUnitRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new DailyReportService(
                _dailyReportRepositoryMock.Object,
                null!,
                _packageRepositoryMock.Object,
                _contractRepositoryMock.Object,
                _sessionRepositoryMock.Object
            );

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenPackageRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new DailyReportService(
                _dailyReportRepositoryMock.Object,
                _unitRepositoryMock.Object,
                null!,
                _contractRepositoryMock.Object,
                _sessionRepositoryMock.Object
            );

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenContractRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new DailyReportService(
                _dailyReportRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _packageRepositoryMock.Object,
                null!,
                _sessionRepositoryMock.Object
            );

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenSessionRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new DailyReportService(
                _dailyReportRepositoryMock.Object,
                _unitRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _contractRepositoryMock.Object,
                null!
            );

            action.Should().Throw<ArgumentNullException>();
        }
    }
}


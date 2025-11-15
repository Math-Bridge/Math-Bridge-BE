using FluentAssertions;
using MathBridgeSystem.Application.DTOs.DailyReport;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class DailyReportServiceForecastAndProgressTests
    {
        private readonly Mock<IDailyReportRepository> _dailyReportRepo;
        private readonly Mock<IUnitRepository> _unitRepo;
        private readonly Mock<IPackageRepository> _packageRepo;
        private readonly DailyReportService _service;

        public DailyReportServiceForecastAndProgressTests()
        {
            _dailyReportRepo = new Mock<IDailyReportRepository>();
            _unitRepo = new Mock<IUnitRepository>();
            _packageRepo = new Mock<IPackageRepository>();
            _service = new DailyReportService(_dailyReportRepo.Object, _unitRepo.Object, _packageRepo.Object);
        }

        [Fact]
        public async Task GetLearningCompletionForecastAsync_ComputesCorrectly()
        {
            var childId = Guid.NewGuid();
            var curriculumId = Guid.NewGuid();
            var startUnitId = Guid.NewGuid();
            var startUnit = new Unit { UnitId = startUnitId, UnitName = "Unit 1", UnitOrder = 1, IsActive = true, Curriculum = new Curriculum{ CurriculumId = curriculumId, CurriculumName = "Math" } };
            var oldestReport = new DailyReport { ReportId = Guid.NewGuid(), ChildId = childId, Child = new Child{ ChildId = childId, FullName = "Child" }, CreatedDate = DateOnly.FromDateTime(DateTime.Today), Unit = startUnit };
            _dailyReportRepo.Setup(r => r.GetOldestByChildIdAsync(childId)).ReturnsAsync(oldestReport);

            var units = new List<Unit>
            {
                startUnit,
                new Unit { UnitId = Guid.NewGuid(), UnitName = "Unit 2", UnitOrder = 2, IsActive = true, Curriculum = new Curriculum{ CurriculumId = curriculumId } },
                new Unit { UnitId = Guid.NewGuid(), UnitName = "Unit 3", UnitOrder = 3, IsActive = true, Curriculum = new Curriculum{ CurriculumId = curriculumId } }
            };
            _unitRepo.Setup(r => r.GetByCurriculumIdAsync(curriculumId)).ReturnsAsync(units);
            _packageRepo.Setup(r => r.GetPackageByCurriculumIdAsync(curriculumId)).ReturnsAsync(new PaymentPackage{ DurationDays = 42 }); // 3 units window

            var dto = await _service.GetLearningCompletionForecastAsync(childId);
            dto.StartingUnitOrder.Should().Be(1);
            dto.TotalUnitsToComplete.Should().Be(3);
            dto.WeeksToCompletion.Should().Be(6.0);
            dto.Message.Should().Contain("Unit 3");
        }

        [Fact]
        public async Task GetLearningCompletionForecastAsync_NoOldestReport_Throws()
        {
            _dailyReportRepo.Setup(r => r.GetOldestByChildIdAsync(It.IsAny<Guid>())).ReturnsAsync((DailyReport)null!);
            await FluentActions.Invoking(() => _service.GetLearningCompletionForecastAsync(Guid.NewGuid()))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetLearningCompletionForecastAsync_NoActiveUnits_Throws()
        {
            var childId = Guid.NewGuid();
            var curriculumId = Guid.NewGuid();
            var startUnit = new Unit { UnitId = Guid.NewGuid(), UnitName = "Unit 1", UnitOrder = 10, IsActive = true, Curriculum = new Curriculum{ CurriculumId = curriculumId } };
            var oldestReport = new DailyReport { ReportId = Guid.NewGuid(), ChildId = childId, Child = new Child{ ChildId = childId, FullName = "Child" }, CreatedDate = DateOnly.FromDateTime(DateTime.Today), Unit = startUnit };
            _dailyReportRepo.Setup(r => r.GetOldestByChildIdAsync(childId)).ReturnsAsync(oldestReport);
            _unitRepo.Setup(r => r.GetByCurriculumIdAsync(curriculumId)).ReturnsAsync(new List<Unit>());
            _packageRepo.Setup(r => r.GetPackageByCurriculumIdAsync(curriculumId)).ReturnsAsync(new PaymentPackage{ DurationDays = 14 });

            await FluentActions.Invoking(() => _service.GetLearningCompletionForecastAsync(childId))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetChildUnitProgressAsync_ComputesUnitsProgress()
        {
            var childId = Guid.NewGuid();
            var curriculumId = Guid.NewGuid();
            var unit1 = new Unit { UnitId = Guid.NewGuid(), UnitName = "U1", UnitOrder = 1, IsActive = true, Curriculum = new Curriculum{ CurriculumId = curriculumId } };
            var unit2 = new Unit { UnitId = Guid.NewGuid(), UnitName = "U2", UnitOrder = 2, IsActive = true, Curriculum = new Curriculum{ CurriculumId = curriculumId } };
            var reports = new List<DailyReport>
            {
                new DailyReport{ ReportId = Guid.NewGuid(), ChildId = childId, Child = new Child{ ChildId = childId, FullName = "Kid" }, CreatedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), UnitId = unit1.UnitId, Unit = unit1, OnTrack = true, HaveHomework = false },
                new DailyReport{ ReportId = Guid.NewGuid(), ChildId = childId, Child = new Child{ ChildId = childId, FullName = "Kid" }, CreatedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)), UnitId = unit1.UnitId, Unit = unit1, OnTrack = true, HaveHomework = true },
                new DailyReport{ ReportId = Guid.NewGuid(), ChildId = childId, Child = new Child{ ChildId = childId, FullName = "Kid" }, CreatedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), UnitId = unit2.UnitId, Unit = unit2, OnTrack = false, HaveHomework = false }
            };
            _dailyReportRepo.Setup(r => r.GetByChildIdAsync(childId)).ReturnsAsync(reports);

            // Forecast dependencies
            _dailyReportRepo.Setup(r => r.GetOldestByChildIdAsync(childId)).ReturnsAsync(new DailyReport{ ReportId = Guid.NewGuid(), ChildId = childId, Child = new Child{ ChildId = childId, FullName = "Kid" }, CreatedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), Unit = unit1 });
            _unitRepo.Setup(r => r.GetByCurriculumIdAsync(curriculumId)).ReturnsAsync(new List<Unit>{ unit1, unit2 });
            _packageRepo.Setup(r => r.GetPackageByCurriculumIdAsync(curriculumId)).ReturnsAsync(new PaymentPackage{ DurationDays = 28 });

            var dto = await _service.GetChildUnitProgressAsync(childId);
            dto.TotalUnitsLearned.Should().Be(2);
            dto.UniqueLessonsCompleted.Should().Be(3);
            dto.UnitsProgress.Should().HaveCount(2);
            dto.UnitsProgress.First().TimesLearned.Should().Be(2);
            dto.UnitsProgress.Any(u => u.HasHomework).Should().BeTrue();
        }

        [Fact]
        public async Task GetChildUnitProgressAsync_NoReports_Throws()
        {
            _dailyReportRepo.Setup(r => r.GetByChildIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<DailyReport>());
            await FluentActions.Invoking(() => _service.GetChildUnitProgressAsync(Guid.NewGuid()))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetChildUnitProgressAsync_ChildNull_Throws()
        {
            var childId = Guid.NewGuid();
            var unit = new Unit { UnitId = Guid.NewGuid(), UnitName = "U1", UnitOrder = 1, IsActive = true, Curriculum = new Curriculum{ CurriculumId = Guid.NewGuid() } };
            var reports = new List<DailyReport>
            {
                new DailyReport{ ReportId = Guid.NewGuid(), ChildId = childId, Child = null! as Child, CreatedDate = DateOnly.FromDateTime(DateTime.Today), UnitId = unit.UnitId, Unit = unit, OnTrack = true }
            };
            _dailyReportRepo.Setup(r => r.GetByChildIdAsync(childId)).ReturnsAsync(reports);

            await FluentActions.Invoking(() => _service.GetChildUnitProgressAsync(childId))
                .Should().ThrowAsync<InvalidOperationException>();
        }
    }
}

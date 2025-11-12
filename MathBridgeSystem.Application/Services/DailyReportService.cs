using MathBridgeSystem.Application.DTOs.DailyReport;
using MathBridgeSystem.Application.DTOs.Progress;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class DailyReportService : IDailyReportService
    {
        private readonly IDailyReportRepository _dailyReportRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IPackageRepository _packageRepository;

        public DailyReportService(IDailyReportRepository dailyReportRepository, IUnitRepository unitRepository, IPackageRepository packageRepository)
        {
            _dailyReportRepository = dailyReportRepository ?? throw new ArgumentNullException(nameof(dailyReportRepository));
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        public async Task<DailyReportDto> GetDailyReportByIdAsync(Guid reportId)
        {
            var dailyReport = await _dailyReportRepository.GetByIdAsync(reportId);
            if (dailyReport == null)
                throw new KeyNotFoundException($"Daily report with ID {reportId} not found.");
            return MapToDto(dailyReport);
        }

        public async Task<IEnumerable<DailyReportDto>> GetDailyReportsByTutorIdAsync(Guid tutorId)
        {
            var dailyReports = await _dailyReportRepository.GetByTutorIdAsync(tutorId);
            return dailyReports.Select(MapToDto);
        }

        public async Task<IEnumerable<DailyReportDto>> GetDailyReportsByChildIdAsync(Guid childId)
        {
            var dailyReports = await _dailyReportRepository.GetByChildIdAsync(childId);
            return dailyReports.Select(MapToDto);
        }

        public async Task<IEnumerable<DailyReportDto>> GetDailyReportsByBookingIdAsync(Guid bookingId)
        {
            var dailyReports = await _dailyReportRepository.GetByBookingIdAsync(bookingId);
            return dailyReports.Select(MapToDto);
        }

        public async Task<LearningCompletionForecastDto> GetLearningCompletionForecastAsync(Guid childId)
        {
            // Get the oldest daily report for the child
            var oldestReport = await _dailyReportRepository.GetOldestByChildIdAsync(childId);
            if (oldestReport == null)
                throw new KeyNotFoundException($"No daily reports found for child with ID {childId}.");

            // Get the starting unit information
            var startingUnit = oldestReport.Unit;
            if (startingUnit == null)
                throw new InvalidOperationException($"Starting unit not found for report with ID {oldestReport.ReportId}.");

            // Get the curriculum to find the last unit
            var curriculum = startingUnit.Curriculum;
            if (curriculum == null)
                throw new InvalidOperationException($"Curriculum not found for unit with ID {startingUnit.UnitId}.");

            // Get all active units in the curriculum ordered by unit order
            var allUnits = await _unitRepository.GetByCurriculumIdAsync(curriculum.CurriculumId);
            var package = await _packageRepository.GetPackageByCurriculumIdAsync(curriculum.CurriculumId);
            var numberOfUnits = package.DurationDays / 14;
            var filteredUnits = allUnits.Where(u => u.UnitOrder >= startingUnit.UnitOrder && u.IsActive)
                .Take(numberOfUnits)
                .OrderBy(u => u.UnitOrder)
                .ToList();
            
            // Find the last unit
            var lastUnit = filteredUnits.MaxBy(u => u.UnitOrder);
            if (lastUnit == null)
                throw new InvalidOperationException($"No active units found in curriculum with ID {curriculum.CurriculumId}.");

            // Calculate completion forecast
            var startDate = oldestReport.CreatedDate;
            var unitsToComplete = lastUnit.UnitOrder - startingUnit.UnitOrder + 1;
            var daysPerUnit = 14; // Average 2 weeks per unit
            var totalDays = unitsToComplete * daysPerUnit;
            var estimatedCompletionDate = startDate.AddDays(totalDays);

            return new LearningCompletionForecastDto
            {
                ChildId = oldestReport.ChildId,
                ChildName = oldestReport.Child.FullName,
                CurriculumId = curriculum.CurriculumId,
                CurriculumName = curriculum.CurriculumName,
                StartingUnitId = startingUnit.UnitId,
                StartingUnitName = startingUnit.UnitName,
                StartingUnitOrder = startingUnit.UnitOrder,
                LastUnitId = lastUnit.UnitId,
                LastUnitName = lastUnit.UnitName,
                LastUnitOrder = lastUnit.UnitOrder,
                TotalUnitsToComplete = unitsToComplete,
                StartDate = startDate,
                EstimatedCompletionDate = estimatedCompletionDate.ToDateTime(TimeOnly.MinValue),
                DaysToCompletion = totalDays,
                WeeksToCompletion = Math.Round(totalDays / 7.0, 2),
                Message = $"Child will complete {lastUnit.UnitName} (Unit {lastUnit.UnitOrder}) by approximately {estimatedCompletionDate:MMMM dd, yyyy}"
            };
        }

        public async Task<ChildUnitProgressDto> GetChildUnitProgressAsync(Guid childId)
        {
            // Get all daily reports for the child sorted by date
            var dailyReports = await _dailyReportRepository.GetByChildIdAsync(childId);
            if (!dailyReports.Any())
                throw new KeyNotFoundException($"No daily reports found for child with ID {childId}.");

            var reportsOrderedByDate = dailyReports.OrderBy(d => d.CreatedDate).ToList();
            
            // Get child and unit information
            var child = reportsOrderedByDate.First().Child;

            if (child == null)
                throw new InvalidOperationException($"Child information not found for daily reports.");
            

            // Group reports by unit
            var unitGroups = reportsOrderedByDate
                .GroupBy(d => d.UnitId)
                .OrderBy(g => g.First().Unit?.UnitOrder ?? 0)
                .ToList();

            var unitsProgress = new List<UnitProgressDetail>();
            var today = DateOnly.FromDateTime(DateTime.Now);

            foreach (var unitGroup in unitGroups)
            {
                var unit = unitGroup.First().Unit;
                if (unit == null)
                    continue;

                var unitReports = unitGroup.ToList();
                var firstLearned = unitReports.Min(r => r.CreatedDate);
                var lastLearned = unitReports.Max(r => r.CreatedDate);
                var daysSinceLearned = (today.DayNumber - lastLearned.DayNumber);

                unitsProgress.Add(new UnitProgressDetail
                {
                    UnitId = unit.UnitId,
                    UnitName = unit.UnitName,
                    UnitOrder = unit.UnitOrder,
                    TimesLearned = unitReports.Count,
                    FirstLearned = firstLearned,
                    LastLearned = lastLearned,
                    DaysSinceLearned = daysSinceLearned,
                    OnTrack = unitReports.All(r => r.OnTrack),
                    HasHomework = unitReports.Any(r => r.HaveHomework)
                });
            }
            var uniqueUnitIdCount = reportsOrderedByDate
                .GroupBy(d => d.UnitId)
                .Count(g => g.Count() == 1);
            var firstReportDate = reportsOrderedByDate.First().CreatedDate;
            var lastReportDate = reportsOrderedByDate.Last().CreatedDate;
            var forecast = await GetLearningCompletionForecastAsync(childId);
            return new ChildUnitProgressDto
            {
                ChildId = child.ChildId,
                ChildName = child.FullName,
                TotalUnitsLearned = unitGroups.Count,
                UniqueLessonsCompleted = reportsOrderedByDate.Count,
                UnitsProgress = unitsProgress,
                FirstLessonDate = firstReportDate,
                LastLessonDate = lastReportDate,
                PercentageOfCurriculumCompleted = Math.Round((double)uniqueUnitIdCount / forecast.TotalUnitsToComplete * 100, 2),
                Message = $"{child.FullName} has learned {unitGroups.Count} units across {reportsOrderedByDate.Count}"
            };
        }

        public async Task<Guid> CreateDailyReportAsync(CreateDailyReportRequest request, Guid tutorId)
        {
            var dailyReport = new DailyReport
            {
                ReportId = Guid.NewGuid(),
                ChildId = request.ChildId,
                TutorId = tutorId,
                BookingId = request.BookingId,
                Notes = request.Notes,
                OnTrack = request.OnTrack,
                HaveHomework = request.HaveHomework,
                CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                UnitId = request.UnitId,
                TestId = request.TestId
            };

            var createdReport = await _dailyReportRepository.AddAsync(dailyReport);
            return createdReport.ReportId;
        }

        public async Task UpdateDailyReportAsync(Guid reportId, UpdateDailyReportRequest request)
        {
            var dailyReport = await _dailyReportRepository.GetByIdAsync(reportId);
            if (dailyReport == null)
                throw new KeyNotFoundException($"Daily report with ID {reportId} not found.");

            if (!string.IsNullOrEmpty(request.Notes))
                dailyReport.Notes = request.Notes;

            if (request.OnTrack.HasValue)
                dailyReport.OnTrack = request.OnTrack.Value;

            if (request.HaveHomework.HasValue)
                dailyReport.HaveHomework = request.HaveHomework.Value;

            if (request.UnitId.HasValue)
                dailyReport.UnitId = request.UnitId.Value;

            if (request.TestId.HasValue)
                dailyReport.TestId = request.TestId.Value;

            await _dailyReportRepository.UpdateAsync(dailyReport);
        }

        public async Task<bool> DeleteDailyReportAsync(Guid reportId)
        {
            return await _dailyReportRepository.DeleteAsync(reportId);
        }

        private DailyReportDto MapToDto(DailyReport dailyReport)
        {
            return new DailyReportDto
            {
                ReportId = dailyReport.ReportId,
                ChildId = dailyReport.ChildId,
                TutorId = dailyReport.TutorId,
                BookingId = dailyReport.BookingId,
                Notes = dailyReport.Notes,
                OnTrack = dailyReport.OnTrack,
                HaveHomework = dailyReport.HaveHomework,
                CreatedDate = dailyReport.CreatedDate,
                UnitId = dailyReport.UnitId,
                TestId = dailyReport.TestId
            };
        }
    }
}

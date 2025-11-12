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

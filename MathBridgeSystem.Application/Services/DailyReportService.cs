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
        private readonly IContractRepository _contractRepository;
        private readonly ISessionRepository _sessionRepository;

        public DailyReportService(IDailyReportRepository dailyReportRepository, IUnitRepository unitRepository, IPackageRepository packageRepository, IContractRepository contractRepository, ISessionRepository sessionRepository)
        {
            _dailyReportRepository = dailyReportRepository ?? throw new ArgumentNullException(nameof(dailyReportRepository));
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task<IEnumerable<DailyReportDto>> GetAllDailyReportsAsync()
        {
            var dailyReports = await _dailyReportRepository.GetAllAsync();
            return dailyReports.Select(MapToDto);
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

        public async Task<LearningCompletionForecastDto> GetLearningCompletionForecastAsync(Guid contractId)
        {
            // 1. Get contract with all related information
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null)
                throw new KeyNotFoundException($"Contract with ID {contractId} not found.");

            if (contract.Child == null)
                throw new InvalidOperationException($"Child information not found for contract {contractId}.");

            if (contract.Package == null)
                throw new InvalidOperationException($"Package information not found for contract {contractId}.");

            var child = contract.Child;
            var package = contract.Package;

            // 2. Get all sessions for this contract
            var allSessions = await _sessionRepository.GetByContractIdAsync(contractId);
            if (!allSessions.Any())
                throw new KeyNotFoundException($"No sessions found for contract {contractId}.");

            var totalSessions = allSessions.Count();

            // 3. Get daily reports for this contract's sessions
            var sessionBookingIds = allSessions.Select(s => s.BookingId).ToList();
            var dailyReports = await _dailyReportRepository.GetByBookingIdsAsync(sessionBookingIds);

            var completedSessions = dailyReports.Count();
            var remainingSessions = totalSessions - completedSessions;

            // 4. Get curriculum and all active units
            var curriculumId = package.CurriculumId;
            var allCurriculumUnits = await _unitRepository.GetByCurriculumIdAsync(curriculumId);
            if (!allCurriculumUnits.Any())
                throw new InvalidOperationException($"No units found in curriculum {curriculumId}.");

            var activeUnits = allCurriculumUnits
                .Where(u => u.IsActive)
                .OrderBy(u => u.UnitOrder)
                .ToList();

            if (!activeUnits.Any())
                throw new InvalidOperationException($"No active units found in curriculum {curriculumId}.");

            var firstUnit = activeUnits.First();
            var lastCurriculumUnit = activeUnits.Last();
            var totalCurriculumUnits = activeUnits.Count;

            // 5. Determine starting unit from the OLDEST daily report
            DateOnly? firstLessonDate = null;
            DateOnly? lastLessonDate = null;
            Unit? startingUnit = null;
            Unit? currentUnit = null;
            int unitsCompletedBeforeStart = 0;

            if (dailyReports.Any())
            {
                var orderedReports = dailyReports.OrderBy(r => r.CreatedDate).ToList();
                firstLessonDate = orderedReports.First().CreatedDate;
                lastLessonDate = orderedReports.Last().CreatedDate;

                // Get the OLDEST report's unit - this is where the student started
                var oldestReportWithUnit = orderedReports.FirstOrDefault(r => r.UnitId != Guid.Empty);
                if (oldestReportWithUnit != null)
                {
                    startingUnit = oldestReportWithUnit.Unit;

                    // If unit is not loaded, fetch it
                    if (startingUnit == null)
                    {
                        startingUnit = await _unitRepository.GetByIdAsync(oldestReportWithUnit.UnitId);
                    }

                    // If starting unit order > 1, count all units before it as completed
                    if (startingUnit != null && startingUnit.UnitOrder > firstUnit.UnitOrder)
                    {
                        unitsCompletedBeforeStart = activeUnits.Count(u => u.UnitOrder < startingUnit.UnitOrder);
                    }
                }

                // Get the LATEST report's unit - this is where the student currently is
                var latestReportWithUnit = orderedReports.LastOrDefault(r => r.UnitId != Guid.Empty);
                if (latestReportWithUnit != null)
                {
                    currentUnit = latestReportWithUnit.Unit;

                    // If unit is not loaded, fetch it
                    if (currentUnit == null)
                    {
                        currentUnit = await _unitRepository.GetByIdAsync(latestReportWithUnit.UnitId);
                    }
                }
            }

            // If no starting unit found in reports, use the first unit of the curriculum
            if (startingUnit == null)
            {
                startingUnit = firstUnit;
            }

            // If no current unit found, use the starting unit
            if (currentUnit == null)
            {
                currentUnit = startingUnit;
            }

            // Ensure curriculum is loaded
            if (currentUnit!.Curriculum == null)
            {
                var unitWithCurriculum = await _unitRepository.GetByIdAsync(currentUnit.UnitId);
                if (unitWithCurriculum?.Curriculum == null)
                    throw new InvalidOperationException("Curriculum information could not be loaded.");
                currentUnit.Curriculum = unitWithCurriculum.Curriculum;
            }

            var curriculum = currentUnit.Curriculum;

            // 6. Calculate units progress and determine if curriculum is completed
            var unitsFromStartToCurrent = currentUnit.UnitOrder - startingUnit!.UnitOrder + 1;
            var totalUnitsCompleted = unitsCompletedBeforeStart + unitsFromStartToCurrent;
            var remainingUnitsInCurriculum = activeUnits.Count(u => u.UnitOrder > currentUnit.UnitOrder);
            var isCurriculumCompleted = remainingUnitsInCurriculum == 0;

            // 7. Calculate estimated last unit based on remaining sessions
            // Estimate: each unit takes approximately (total sessions / total curriculum units) sessions
            Unit lastUnit;
            int totalUnitsToComplete;

            if (isCurriculumCompleted)
            {
                // Student has completed all units in the curriculum
                lastUnit = lastCurriculumUnit;
                totalUnitsToComplete = totalCurriculumUnits;
            }
            else
            {
                // Calculate how many more units can be completed with remaining sessions
                var sessionsPerUnit = totalSessions > 0 && totalCurriculumUnits > 0
                    ? Math.Max(1.0, (double)totalSessions / totalCurriculumUnits)
                    : 2.0; // Default: 2 sessions per unit

                var estimatedAdditionalUnits = remainingSessions > 0
                    ? (int)Math.Floor(remainingSessions / sessionsPerUnit)
                    : 0;

                var estimatedLastUnitOrder = Math.Min(
                    currentUnit.UnitOrder + estimatedAdditionalUnits,
                    lastCurriculumUnit.UnitOrder);

                lastUnit = activeUnits.FirstOrDefault(u => u.UnitOrder == estimatedLastUnitOrder) ?? lastCurriculumUnit;
                totalUnitsToComplete = lastUnit.UnitOrder - startingUnit.UnitOrder + 1 + unitsCompletedBeforeStart;
            }

            // 8. Calculate progress percentage based on units
            var unitProgressPercentage = totalCurriculumUnits > 0
                ? Math.Round((double)totalUnitsCompleted / totalCurriculumUnits * 100, 2)
                : 0.0;

            // Cap at 100% if curriculum is completed
            if (isCurriculumCompleted || unitProgressPercentage > 100)
            {
                unitProgressPercentage = 100.0;
            }

            // 9. Calculate estimated completion date
            DateTime estimatedCompletionDate;
            int daysToCompletion;

            if (isCurriculumCompleted)
            {
                // Curriculum already completed
                daysToCompletion = 0;
                estimatedCompletionDate = lastLessonDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
            }
            else if (completedSessions > 0 && remainingSessions > 0)
            {
                // Calculate average days per session from actual progress
                var daysElapsed = lastLessonDate!.Value.DayNumber - firstLessonDate!.Value.DayNumber;
                var averageDaysPerSession = daysElapsed > 0 && completedSessions > 1
                    ? (double)daysElapsed / (completedSessions - 1)
                    : package.SessionsPerWeek > 0 ? 7.0 / package.SessionsPerWeek : 3.5;

                daysToCompletion = (int)Math.Ceiling(remainingSessions * averageDaysPerSession);
                estimatedCompletionDate = (lastLessonDate ?? DateOnly.FromDateTime(DateTime.Today))
                    .AddDays(daysToCompletion)
                    .ToDateTime(TimeOnly.MinValue);
            }
            else if (completedSessions == 0)
            {
                // No sessions completed yet, use contract dates
                daysToCompletion = contract.EndDate.DayNumber - contract.StartDate.DayNumber;
                estimatedCompletionDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                // All sessions completed
                daysToCompletion = 0;
                estimatedCompletionDate = lastLessonDate!.Value.ToDateTime(TimeOnly.MinValue);
            }

            // 10. Build result message
            string message;
            if (completedSessions == 0)
            {
                message = $"{child.FullName} has not started the contract yet. Expected to complete Unit {lastUnit.UnitOrder} ({lastUnit.UnitName ?? "Final"}) by {estimatedCompletionDate:MMMM dd, yyyy}.";
            }
            else if (isCurriculumCompleted)
            {
                message = $"{child.FullName} has completed all {totalCurriculumUnits} units in the curriculum (100%). " +
                          $"{(remainingSessions > 0 ? $"Remaining {remainingSessions} sessions will be review/practice sessions." : "All sessions completed.")}";
            }
            else if (remainingSessions == 0)
            {
                message = $"{child.FullName} has completed all {totalSessions} sessions for this contract. " +
                          $"Currently at Unit {currentUnit.UnitOrder} ({currentUnit.UnitName ?? "Current"}) with {unitProgressPercentage}% curriculum progress.";
            }
            else
            {
                message = $"{child.FullName} has completed {completedSessions} of {totalSessions} sessions. " +
                          $"Currently at Unit {currentUnit.UnitOrder} ({currentUnit.UnitName ?? "Current"}) with {unitProgressPercentage}% curriculum progress. " +
                          $"Expected to finish Unit {lastUnit.UnitOrder} ({lastUnit.UnitName ?? "Final"}) around {estimatedCompletionDate:MMMM dd, yyyy}.";
            }

            return new LearningCompletionForecastDto
            {
                ContractId = contractId,
                ChildId = child.ChildId,
                ChildName = child.FullName ?? "Unknown Child",
                CurriculumId = curriculum.CurriculumId,
                CurriculumName = curriculum.CurriculumName ?? "Unknown Curriculum",
                PackageId = package.PackageId,
                PackageName = package.PackageName ?? "Unknown Package",
                CurrentUnitId = currentUnit.UnitId,
                CurrentUnitName = currentUnit.UnitName ?? "Unit",
                CurrentUnitOrder = currentUnit.UnitOrder,
                EstimatedLastUnitId = lastUnit.UnitId,
                EstimatedLastUnitName = lastUnit.UnitName ?? "Final Unit",
                EstimatedLastUnitOrder = lastUnit.UnitOrder,
                TotalUnitsToComplete = totalUnitsToComplete,
                CompletedSessions = completedSessions,
                TotalSessions = totalSessions,
                RemainingSessions = remainingSessions,
                ContractStartDate = contract.StartDate,
                ContractEndDate = contract.EndDate,
                FirstLessonDate = firstLessonDate,
                LastLessonDate = lastLessonDate,
                EstimatedCompletionDate = estimatedCompletionDate,
                DaysToCompletion = daysToCompletion,
                WeeksToCompletion = Math.Round(daysToCompletion / 7.0, 2),
                ProgressPercentage = unitProgressPercentage,
                Message = message
            };
        }

        public async Task<ChildUnitProgressDto> GetChildUnitProgressAsync(Guid contractId)
        {
            // 1. Get contract with child information
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract?.Child == null)
                throw new KeyNotFoundException($"Contract {contractId} or child information not found.");
            
            var child = contract.Child;

            // 2. Get all sessions for the contract
            var allSessions = await _sessionRepository.GetByContractIdAsync(contractId);
            
            if (!allSessions.Any())
                throw new KeyNotFoundException($"No sessions found for contract {contractId}.");

            // 3. Get daily reports for this child and filter to this contract's sessions
            var dailyReports = await _dailyReportRepository.GetByChildIdAsync(child.ChildId);
            
            var sessionIdsInContract = allSessions.Select(s => s.BookingId).ToHashSet();
            var contractDailyReports = dailyReports
                .Where(dr => sessionIdsInContract.Contains(dr.BookingId))
                .OrderBy(r => r.CreatedDate)
                .ToList();
            
            if (!contractDailyReports.Any())
                throw new KeyNotFoundException($"No daily reports found for contract {contractId}.");

            // 4. Group by Unit (skip reports where Unit is null)
            var unitGroups = contractDailyReports
                .Where(r => r.Unit != null)
                .GroupBy(r => r.UnitId)
                .OrderBy(g => g.First().Unit!.UnitOrder)
                .ToList();

            var unitsProgress = new List<UnitProgressDetail>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var group in unitGroups)
            {
                var unit = group.First().Unit!;
                var reportsInUnit = group.ToList();

                var firstLearned = reportsInUnit.Min(r => r.CreatedDate);
                var lastLearned = reportsInUnit.Max(r => r.CreatedDate);

                unitsProgress.Add(new UnitProgressDetail
                {
                    UnitId = unit.UnitId,
                    UnitName = string.IsNullOrWhiteSpace(unit.UnitName) ? "Untitled Unit" : unit.UnitName,
                    UnitOrder = unit.UnitOrder,
                    TimesLearned = reportsInUnit.Count,
                    FirstLearned = firstLearned,
                    LastLearned = lastLearned,
                    DaysSinceLearned = Math.Max(0, today.DayNumber - lastLearned.DayNumber),
                    OnTrack = reportsInUnit.All(r => r.OnTrack),
                    HasHomework = reportsInUnit.Any(r => r.HaveHomework)
                });
            }

            // 5. Calculate progress percentage based on sessions completed vs total sessions
            var completedSessionsCount = contractDailyReports.Count;
            var totalSessionsCount = allSessions.Count;
            
            var percentage = totalSessionsCount > 0
                ? Math.Round((double)completedSessionsCount / totalSessionsCount * 100, 2)
                : 0.0;

            // 6. Return final DTO
            return new ChildUnitProgressDto
            {
                ChildId = child.ChildId,
                ChildName = child.FullName,
                TotalUnitsLearned = unitGroups.Count,
                UniqueLessonsCompleted = completedSessionsCount,
                UnitsProgress = unitsProgress,
                FirstLessonDate = contractDailyReports.First().CreatedDate,
                LastLessonDate = contractDailyReports.Last().CreatedDate,
                PercentageOfCurriculumCompleted = percentage,
                Message = $"{child.FullName} has completed {completedSessionsCount} out of {totalSessionsCount} sessions ({percentage}%) for contract {contractId}."
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
                Url = request.Url
            };

            var createdReport = await _dailyReportRepository.AddAsync(dailyReport);
            
            // Check if this is the last session and update contract status if needed
            var session = await _sessionRepository.GetByIdAsync(request.BookingId);
            if (session != null)
            {
                var contract = await _contractRepository.GetByIdAsync(session.ContractId);
                if (contract != null)
                {
                    // Get all sessions for this contract
                    var allSessions = await _sessionRepository.GetByContractIdAsync(session.ContractId);
                    
                    // Check if there are any sessions after the current session date
                    var hasRemainingsessions = allSessions.Any(s => s.SessionDate > session.SessionDate);
                    
                    // If no remaining sessions, mark contract as completed
                    if (!hasRemainingsessions)
                    {
                        contract.Status = "Completed";
                        await _contractRepository.UpdateAsync(contract);
                    }
                }
            }
            
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
                Url = dailyReport.Url
            };
        }

        public async Task<IEnumerable<DailyReportDto>> GetDailyReportsByContractIdAsync(Guid contractId)
        {
            // First get all session BookingIds for this contract
            var sessionBookingIds = await _sessionRepository
                .GetByContractIdAsync(contractId)
                .ContinueWith(t => t.Result.Select(s => s.BookingId).ToList());

            if (!sessionBookingIds.Any())
                throw new KeyNotFoundException($"No sessions found for contract {contractId}.");

            // Then get daily reports for those booking IDs
            var reports = await _dailyReportRepository.GetByBookingIdsAsync(sessionBookingIds);

            if (!reports.Any())
                throw new KeyNotFoundException($"No daily reports found for contract {contractId}.");

            return reports.Select(MapToDto).OrderByDescending(r => r.CreatedDate);
        }
    }
}

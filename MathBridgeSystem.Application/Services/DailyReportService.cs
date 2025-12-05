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

            // If no daily reports exist yet, use the contract start date as reference
            DateOnly? firstLessonDate = null;
            DateOnly? lastLessonDate = null;
            Guid? currentUnitId = null;
            Unit? currentUnit = null;

            if (dailyReports.Any())
            {
                var orderedReports = dailyReports.OrderBy(r => r.CreatedDate).ToList();
                firstLessonDate = orderedReports.First().CreatedDate;
                lastLessonDate = orderedReports.Last().CreatedDate;

                // Get the most recent unit being studied
                var latestReportWithUnit = orderedReports.LastOrDefault(r => r.UnitId != Guid.Empty);
                if (latestReportWithUnit != null)
                {
                    currentUnitId = latestReportWithUnit.UnitId;
                    currentUnit = latestReportWithUnit.Unit;
                    
                    // If unit is not loaded, fetch it
                    if (currentUnit == null)
                    {
                        currentUnit = await _unitRepository.GetByIdAsync(currentUnitId.Value);
                    }
                }
            }

            // If no unit found in reports, get the first unit of the curriculum
            if (currentUnit == null)
            {
                var curriculumId = package.CurriculumId;
                var allUnits = await _unitRepository.GetByCurriculumIdAsync(curriculumId);
                if (!allUnits.Any())
                    throw new InvalidOperationException($"No units found in curriculum {curriculumId}.");

                currentUnit = allUnits
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.UnitOrder)
                    .FirstOrDefault();

                if (currentUnit == null)
                    throw new InvalidOperationException($"No active units found in curriculum {curriculumId}.");
            }

            // Ensure curriculum is loaded
            if (currentUnit.Curriculum == null)
            {
                var unitWithCurriculum = await _unitRepository.GetByIdAsync(currentUnit.UnitId);
                if (unitWithCurriculum?.Curriculum == null)
                    throw new InvalidOperationException("Curriculum information could not be loaded.");
                currentUnit.Curriculum = unitWithCurriculum.Curriculum;
            }

            var curriculum = currentUnit.Curriculum;

            // 4. Get all active units in the curriculum starting from current unit
            var allCurriculumUnits = await _unitRepository.GetByCurriculumIdAsync(curriculum.CurriculumId);
            var activeUnits = allCurriculumUnits
                .Where(u => u.IsActive && u.UnitOrder >= currentUnit.UnitOrder)
                .OrderBy(u => u.UnitOrder)
                .ToList();

            if (!activeUnits.Any())
                throw new InvalidOperationException("No active units found from the current learning position.");

            // 5. Calculate estimated units to cover based on package duration
            // Assuming 2 sessions per week and each unit takes approximately 2 weeks
            var estimatedUnitsFromDuration = package.DurationDays / 14; // 14 days = 2 weeks per unit
            
            // Take the units that are expected to be covered
            var unitsToComplete = activeUnits
                .Take(Math.Max(1, estimatedUnitsFromDuration))
                .ToList();

            var lastUnit = unitsToComplete.LastOrDefault() ?? activeUnits.Last();
            var totalUnitsToComplete = lastUnit.UnitOrder - currentUnit.UnitOrder + 1;

            // 6. Calculate estimated completion date
            // Use actual progress if available, otherwise use contract dates
            DateTime estimatedCompletionDate;
            int daysToCompletion;
            
            if (completedSessions > 0 && remainingSessions > 0)
            {
                // Calculate average days per session from actual progress
                var daysElapsed = (lastLessonDate!.Value.DayNumber - firstLessonDate!.Value.DayNumber);
                var averageDaysPerSession = daysElapsed > 0 && completedSessions > 1
                    ? (double)daysElapsed / (completedSessions - 1)
                    : package.SessionsPerWeek > 0 ? 7.0 / package.SessionsPerWeek : 3.5; // Default to ~2 sessions per week

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

            // 7. Calculate progress percentage
            var progressPercentage = totalSessions > 0
                ? Math.Round((double)completedSessions / totalSessions * 100, 2)
                : 0.0;

            // 8. Build result message
            string message;
            if (completedSessions == 0)
            {
                message = $"{child.FullName} has not started the contract yet. Expected to complete Unit {lastUnit.UnitOrder} ({lastUnit.UnitName ?? "Final"}) by {estimatedCompletionDate:MMMM dd, yyyy}.";
            }
            else if (remainingSessions == 0)
            {
                message = $"{child.FullName} has completed all {totalSessions} sessions for this contract. Currently at Unit {currentUnit.UnitOrder} ({currentUnit.UnitName ?? "Current"}).";
            }
            else
            {
                message = $"{child.FullName} has completed {completedSessions} of {totalSessions} sessions ({progressPercentage}%). Expected to finish Unit {lastUnit.UnitOrder} ({lastUnit.UnitName ?? "Final"}) around {estimatedCompletionDate:MMMM dd, yyyy}.";
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
                ProgressPercentage = progressPercentage,
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

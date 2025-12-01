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
            var oldestReport = await _dailyReportRepository.GetOldestByChildIdAsync(childId)
                ?? throw new KeyNotFoundException($"No daily reports found for child {childId}");

            if (oldestReport.Unit == null)
                throw new InvalidOperationException("Oldest report has no Unit assigned.");

            var startingUnit = oldestReport.Unit;

           
            if (startingUnit.Curriculum == null)
            {
                var unitFromDb = await _unitRepository.GetByIdAsync(startingUnit.UnitId);
                if (unitFromDb?.Curriculum == null)
                    throw new InvalidOperationException("Curriculum not found for the starting unit.");
                startingUnit.Curriculum = unitFromDb.Curriculum;
            }

            var curriculum = startingUnit.Curriculum;

            var allUnits = await _unitRepository.GetByCurriculumIdAsync(curriculum.CurriculumId);
            if (!allUnits.Any())
                throw new InvalidOperationException("No units found in this curriculum.");

            var package = await _packageRepository.GetPackageByCurriculumIdAsync(curriculum.CurriculumId)
                ?? throw new InvalidOperationException("Package not found for this curriculum.");

            var numberOfUnits = package.DurationDays / 14;

            var candidateUnits = allUnits
                .Where(u => u.IsActive && u.UnitOrder >= startingUnit.UnitOrder)
                .OrderBy(u => u.UnitOrder)
                .Take(numberOfUnits)
                .ToList();

            if (!candidateUnits.Any())
                throw new InvalidOperationException("No active units found after starting unit.");

            var lastUnit = candidateUnits.MaxBy(u => u.UnitOrder)!;

            var unitsToComplete = lastUnit.UnitOrder - startingUnit.UnitOrder + 1;
            var totalDays = unitsToComplete * 14;
            var estimatedCompletionDate = oldestReport.CreatedDate.AddDays(totalDays);

            string childName = "Unknown Child";
            if (oldestReport.Child != null)
                childName = oldestReport.Child.FullName ?? "Unknown Child";

            return new LearningCompletionForecastDto
            {
                ChildId = oldestReport.ChildId,
                ChildName = childName,
                CurriculumId = curriculum.CurriculumId,
                CurriculumName = curriculum.CurriculumName ?? "Unknown Curriculum",
                StartingUnitId = startingUnit.UnitId,
                StartingUnitName = startingUnit.UnitName ?? "Unit",
                StartingUnitOrder = startingUnit.UnitOrder,
                LastUnitId = lastUnit.UnitId,
                LastUnitName = lastUnit.UnitName ?? "Final Unit",
                LastUnitOrder = lastUnit.UnitOrder,
                TotalUnitsToComplete = unitsToComplete,
                StartDate = oldestReport.CreatedDate,
                EstimatedCompletionDate = estimatedCompletionDate.ToDateTime(TimeOnly.MinValue),
                DaysToCompletion = totalDays,
                WeeksToCompletion = Math.Round(totalDays / 7.0, 2),
                Message = $"Expected to finish Unit {lastUnit.UnitOrder} ({lastUnit.UnitName ?? "Final"}) around {estimatedCompletionDate:MMMM dd, yyyy}"
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

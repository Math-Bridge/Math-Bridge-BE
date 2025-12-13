using MathBridgeSystem.Application.DTOs.DailyReport;
using MathBridgeSystem.Application.DTOs.Progress;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IDailyReportService
    {
        Task<IEnumerable<DailyReportDto>> GetAllDailyReportsAsync();
        Task<DailyReportDto> GetDailyReportByIdAsync(Guid reportId);
        Task<IEnumerable<DailyReportDto>> GetDailyReportsByTutorIdAsync(Guid tutorId);
        Task<IEnumerable<DailyReportDto>> GetDailyReportsByChildIdAsync(Guid childId);
        Task<IEnumerable<DailyReportDto>> GetDailyReportsByBookingIdAsync(Guid bookingId);
        /// <summary>
        /// Gets the learning completion forecast for a specific contract.
        /// Forecasts the expected curriculum completion based on the contract's package, sessions, and progress.
        /// </summary>
        Task<LearningCompletionForecastDto> GetLearningCompletionForecastAsync(Guid contractId);
        /// <summary>
        /// Gets the unit progress for a contract's child based on session completion.
        /// </summary>
        Task<ChildUnitProgressDto> GetChildUnitProgressAsync(Guid contractId);
        Task<object> CreateDailyReportAsync(CreateDailyReportRequest request, Guid tutorId);
        Task UpdateDailyReportAsync(Guid reportId, UpdateDailyReportRequest request);
        Task<bool> DeleteDailyReportAsync(Guid reportId);
        /// <summary>
        /// Gets all daily reports belonging to sessions of a specific contract
        /// </summary>
        Task<IEnumerable<DailyReportDto>> GetDailyReportsByContractIdAsync(Guid contractId);
    }
}

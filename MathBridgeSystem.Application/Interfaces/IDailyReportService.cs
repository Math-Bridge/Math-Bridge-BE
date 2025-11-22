using MathBridgeSystem.Application.DTOs.DailyReport;
using MathBridgeSystem.Application.DTOs.Progress;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IDailyReportService
    {
        Task<DailyReportDto> GetDailyReportByIdAsync(Guid reportId);
        Task<IEnumerable<DailyReportDto>> GetDailyReportsByTutorIdAsync(Guid tutorId);
        Task<IEnumerable<DailyReportDto>> GetDailyReportsByChildIdAsync(Guid childId);
        Task<IEnumerable<DailyReportDto>> GetDailyReportsByBookingIdAsync(Guid bookingId);
        Task<LearningCompletionForecastDto> GetLearningCompletionForecastAsync(Guid childId);
        /// <summary>
        /// Gets the unit progress for a contract's child based on session completion.
        /// </summary>
        Task<ChildUnitProgressDto> GetChildUnitProgressAsync(Guid contractId);
        Task<Guid> CreateDailyReportAsync(CreateDailyReportRequest request, Guid tutorId);
        Task UpdateDailyReportAsync(Guid reportId, UpdateDailyReportRequest request);
        Task<bool> DeleteDailyReportAsync(Guid reportId);
    }
}

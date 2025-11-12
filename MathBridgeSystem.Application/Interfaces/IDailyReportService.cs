using MathBridgeSystem.Application.DTOs.DailyReport;
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
        Task<Guid> CreateDailyReportAsync(CreateDailyReportRequest request, Guid tutorId);
        Task UpdateDailyReportAsync(Guid reportId, UpdateDailyReportRequest request);
        Task<bool> DeleteDailyReportAsync(Guid reportId);
    }
}


using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IDailyReportRepository
    {
        Task<DailyReport> GetByIdAsync(Guid reportId);
        Task<IEnumerable<DailyReport>> GetByTutorIdAsync(Guid tutorId);
        Task<IEnumerable<DailyReport>> GetByChildIdAsync(Guid childId);
        Task<IEnumerable<DailyReport>> GetByBookingIdAsync(Guid bookingId);
        Task<DailyReport> GetOldestByChildIdAsync(Guid childId);
        Task<DailyReport> AddAsync(DailyReport dailyReport);
        Task<DailyReport> UpdateAsync(DailyReport dailyReport);
        Task<bool> DeleteAsync(Guid reportId);
        Task<Unit?> GetUnitByIdAsync(Guid unitId);
    }
}

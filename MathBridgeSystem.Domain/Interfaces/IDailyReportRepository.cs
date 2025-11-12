using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IDailyReportRepository
    {
        Task<DailyReport> GetByIdAsync(Guid reportId);
        Task<IEnumerable<DailyReport>> GetByTutorIdAsync(Guid tutorId);
        Task<IEnumerable<DailyReport>> GetByChildIdAsync(Guid childId);
        Task<IEnumerable<DailyReport>> GetByBookingIdAsync(Guid bookingId);
        Task<DailyReport> AddAsync(DailyReport dailyReport);
        Task<DailyReport> UpdateAsync(DailyReport dailyReport);
        Task<bool> DeleteAsync(Guid reportId);
    }
}


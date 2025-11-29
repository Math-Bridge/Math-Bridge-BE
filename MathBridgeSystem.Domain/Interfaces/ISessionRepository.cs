using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ISessionRepository
    {
        Task AddRangeAsync(IEnumerable<Session> sessions);
        Task<Session?> GetByIdAsync(Guid bookingId);
        Task UpdateAsync(Session session);
        Task<bool> IsTutorAvailableAsync(Guid tutorId, DateOnly date, DateTime startTime, DateTime endTime);
        Task<List<Session>> GetByParentIdAsync(Guid parentId);
        Task<List<Session>> GetByContractIdAsync(Guid contractId);
        Task<List<Session>> GetSessionsInTimeRangeAsync(DateTime startTime, DateTime endTime);
        Task<List<Session>> GetByChildIdAsync(Guid childId, Guid parentId);
        Task<List<Session>> GetByTutorIdAsync(Guid tutorId);
        Task<List<Session>> GetAllSessionsInTimeRangeAsync(DateTime startTime, DateTime endTime);
        Task<List<Session>> GetUpcomingSessionsByContractIdAsync(Guid contractId, DateOnly fromDate);
    }
}
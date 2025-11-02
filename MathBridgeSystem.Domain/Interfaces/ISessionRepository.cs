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
        Task<bool> IsTutorAvailableAsync(Guid tutorId, DateOnly date, TimeSpan startTime, TimeSpan endTime);
    }
}
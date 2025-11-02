using MathBridgeSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IRescheduleRequestRepository
    {
        Task AddAsync(RescheduleRequest entity);
        Task<RescheduleRequest?> GetByIdWithDetailsAsync(Guid id);
        Task UpdateAsync(RescheduleRequest entity);
        Task<bool> HasPendingRequestForBookingAsync(Guid bookingId);
    }
}
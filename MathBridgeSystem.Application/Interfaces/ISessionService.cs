using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ISessionService
    {
        Task<List<SessionDto>> GetSessionsByParentAsync(Guid parentId);
        Task<SessionDto?> GetSessionByIdAsync(Guid bookingId, Guid parentId);
        Task<List<SessionDto>> GetSessionsByChildIdAsync(Guid childId, Guid parentId);
        Task<List<SessionDto>> GetSessionsByTutorIdAsync(Guid tutorId); // MỚI
        Task<bool> UpdateSessionTutorAsync(Guid bookingId, Guid newTutorId, Guid requesterId);

        Task<bool> UpdateSessionStatusAsync(Guid bookingId, string newStatus, Guid tutorId);
        Task<SessionDto?> GetSessionForTutorCheckAsync(Guid bookingId, Guid tutorId);
    }
}
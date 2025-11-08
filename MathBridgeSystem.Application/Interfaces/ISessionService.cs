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
        Task<List<SessionDto>> GetSessionsByMainTutorIdAsync(Guid tutorId);
        Task<List<SessionDto>> GetSessionsBySubstituteTutorIdAsync(Guid tutorId);
        Task<bool> UpdateSessionStatusAsync(Guid bookingId, string newStatus, Guid tutorId);
        Task<SessionDto?> GetSessionForTutorCheckAsync(Guid bookingId, Guid tutorId);
    }
}
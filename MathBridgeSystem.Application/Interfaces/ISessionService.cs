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
        Task<List<SessionDto>> GetSessionsByChildIdAsync(Guid childId);
        Task<List<SessionDto>> GetSessionsByTutorIdAsync(Guid tutorId); 
        Task<bool> UpdateSessionTutorAsync(Guid bookingId, Guid newTutorId, Guid requesterId);

        Task<bool> UpdateSessionStatusAsync(Guid bookingId, string newStatus, Guid tutorId);
        Task<SessionDto?> GetSessionForTutorCheckAsync(Guid bookingId, Guid tutorId);
        Task<SessionDto?> GetSessionByBookingIdAsync(Guid bookingId, Guid userId, string role);
        Task<bool> ChangeSessionTutorAsync(ChangeSessionTutorRequest request, Guid staffId);
        Task<object> GetReplacementTutorsAsync(Guid bookingId);
        /// <summary>
        /// Returns a complete replacement plan when the main tutor is banned or inactive.
        /// Prioritizes promoting a substitute tutor and automatically suggests a new substitute.
        /// </summary>
        Task<object> GetMainTutorReplacementPlanAsync(Guid contractId);

        /// <summary>
        /// Executes the replacement of the main tutor for all remaining sessions.
        /// Updates both Contract (MainTutorId + Substitute slots) and all future Sessions.
        /// </summary>
        Task<bool> ExecuteMainTutorReplacementAsync(Guid contractId, Guid newMainTutorId, Guid newSubstituteTutorId, Guid staffId);
    }
}
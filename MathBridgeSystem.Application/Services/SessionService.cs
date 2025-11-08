using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;

        public SessionService(ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task<List<SessionDto>> GetSessionsByParentAsync(Guid parentId)
        {
            var sessions = await _sessionRepository.GetByParentIdAsync(parentId);
            return MapSessionsToDto(sessions);
        }

        public async Task<SessionDto?> GetSessionByIdAsync(Guid bookingId, Guid parentId)
        {
            var session = await _sessionRepository.GetByIdAsync(bookingId);
            if (session == null || session.Contract.ParentId != parentId)
                return null;

            return MapSessionToDto(session);
        }

        public async Task<List<SessionDto>> GetSessionsByChildIdAsync(Guid childId, Guid parentId)
        {
            var sessions = await _sessionRepository.GetByChildIdAsync(childId, parentId);
            return MapSessionsToDto(sessions);
        }

        public async Task<List<SessionDto>> GetSessionsByTutorIdAsync(Guid tutorId)
        {
            var sessions = await _sessionRepository.GetByTutorIdAsync(tutorId);
            return MapSessionsToDto(sessions);
        }

        private List<SessionDto> MapSessionsToDto(List<Session> sessions)
        {
            return sessions.Select(MapSessionToDto).ToList();
        }

        private SessionDto MapSessionToDto(Session s)
        {
            return new SessionDto
            {
                BookingId = s.BookingId,
                ContractId = s.ContractId,
                SessionDate = s.SessionDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                TutorName = s.Tutor.FullName,
                IsOnline = s.IsOnline,
                VideoCallPlatform = s.VideoCallPlatform,
                OfflineAddress = s.OfflineAddress,
                Status = s.Status,
                ChildName = s.Contract.Child.FullName,
                PackageName = s.Contract.Package.PackageName
            };
        }


        public async Task<bool> UpdateSessionTutorAsync(Guid bookingId, Guid newTutorId, Guid requesterId)
        {
            // Get the session with contract details
            var session = await _sessionRepository.GetByIdAsync(bookingId);
            if (session == null)
                throw new KeyNotFoundException("Session not found.");

            // Get the contract to check available tutors
            var contract = session.Contract;
            if (contract == null)
                throw new InvalidOperationException("Session contract not found.");
            
            // Validate that the new tutor is one of the 3 tutors from the contract
            var validTutorIds = new List<Guid?> 
            { 
                contract.MainTutorId, 
                contract.SubstituteTutor1Id, 
                contract.SubstituteTutor2Id 
            }.Where(id => id.HasValue).Select(id => id.Value).ToList();

            if (!validTutorIds.Contains(newTutorId))
            {
                throw new ArgumentException(
                    "The selected tutor is not assigned to this contract. " +
                    "Only the main tutor or substitute tutors can be assigned to sessions.");
            }

            // Check if the session is in a status that allows tutor changes
            var currentStatus = session.Status.ToLower();
            if (currentStatus == "completed" || currentStatus == "cancelled")
            {
                throw new InvalidOperationException(
                    $"Cannot update tutor for a session with status '{currentStatus}'. " +
                    "Only pending or processing sessions can have tutor changes.");
            }

            // Check if the new tutor is available at the session time
            var isAvailable = await _sessionRepository.IsTutorAvailableAsync(
                newTutorId, 
                session.SessionDate, 
                session.StartTime, 
                session.EndTime);

            if (!isAvailable)
            {
                throw new InvalidOperationException(
                    "The selected tutor is not available at the scheduled session time. " +
                    "Please choose another tutor or reschedule the session.");
            }

            // Update the tutor
            session.TutorId = newTutorId;
            session.UpdatedAt = DateTime.UtcNow;
            
            await _sessionRepository.UpdateAsync(session);
            return true;
        }

        public async Task<bool> UpdateSessionStatusAsync(Guid bookingId, string newStatus, Guid tutorId)
        {
            var session = await _sessionRepository.GetByIdAsync(bookingId);
            if (session == null)
                throw new KeyNotFoundException("Session not found.");
            if (session.TutorId != tutorId)
                throw new UnauthorizedAccessException("You are not the tutor of this session.");

            var currentStatus = session.Status.ToLower();
            var normalizedNewStatus = newStatus.ToLower();

            if (currentStatus != "processing")
                throw new InvalidOperationException($"Session must be in 'processing' status to update. Current: {currentStatus}");

            if (normalizedNewStatus != "completed" && normalizedNewStatus != "cancelled")
                throw new ArgumentException("Status must be 'completed' or 'cancelled'");

            session.Status = normalizedNewStatus;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            return true;
        }

        public async Task<SessionDto?> GetSessionForTutorCheckAsync(Guid bookingId, Guid tutorId)
        {
            var session = await _sessionRepository.GetByIdAsync(bookingId);
            if (session == null || session.TutorId != tutorId)
                return null;

            return new SessionDto
            {
                BookingId = session.BookingId,
                TutorName = session.Tutor.FullName,
                Status = session.Status
            };
        }
        public async Task<SessionDto?> GetSessionByBookingIdAsync(Guid bookingId, Guid userId, string role)
        {
            var session = await _sessionRepository.GetByIdAsync(bookingId);
            if (session == null)
                return null;

            if (role == "parent" && session.Contract.ParentId != userId)
                return null;
            if (role == "tutor" && session.TutorId != userId)
                return null;
            // staff: được xem tất cả

            return MapSessionToDto(session);
        }
    }
}
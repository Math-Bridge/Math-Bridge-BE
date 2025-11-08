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
            return sessions.Select(s => new SessionDto
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
                Status = s.Status
            }).ToList();
        }

        public async Task<SessionDto?> GetSessionByIdAsync(Guid bookingId, Guid parentId)
        {
            var session = await _sessionRepository.GetByIdAsync(bookingId);
            if (session == null || session.Contract.ParentId != parentId)
                return null;

            return new SessionDto
            {
                BookingId = session.BookingId,
                ContractId = session.ContractId,
                SessionDate = session.SessionDate,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                TutorName = session.Tutor.FullName,
                IsOnline = session.IsOnline,
                VideoCallPlatform = session.VideoCallPlatform,
                OfflineAddress = session.OfflineAddress,
                Status = session.Status
            };
        }
        public async Task<List<SessionDto>> GetSessionsByChildIdAsync(Guid childId, Guid parentId)
        {
            var sessions = await _sessionRepository.GetByChildIdAsync(childId, parentId);
            return sessions.Select(s => new SessionDto
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
                Status = s.Status
            }).ToList();
        }
        public async Task<List<SessionDto>> GetSessionsByMainTutorIdAsync(Guid tutorId)
        {
            var sessions = await _sessionRepository.GetByMainTutorIdAsync(tutorId);
            return MapSessionsToDto(sessions);
        }

        public async Task<List<SessionDto>> GetSessionsBySubstituteTutorIdAsync(Guid tutorId)
        {
            var sessions = await _sessionRepository.GetBySubstituteTutorIdAsync(tutorId);
            return MapSessionsToDto(sessions);
        }

        private List<SessionDto> MapSessionsToDto(List<Session> sessions)
        {
            return sessions.Select(s => new SessionDto
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
            }).ToList();
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
    }
}
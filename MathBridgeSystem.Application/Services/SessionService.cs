using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IUserRepository _userRepository;
        private ISessionRepository @object;
        private readonly IChildRepository _childRepository;

        public SessionService(ISessionRepository sessionRepository, IUserRepository userRepository, IChildRepository childRepository)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _childRepository = childRepository ?? throw new ArgumentNullException(nameof(childRepository));
        }

        public SessionService(ISessionRepository @object)
        {
            this.@object = @object;
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

        public async Task<List<SessionDto>> GetSessionsByChildIdAsync(Guid childId)
        {
            var child = await _childRepository.GetByIdAsync(childId);
            var sessions = await _sessionRepository.GetByChildIdAsync(childId, child.ParentId);
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
            session.UpdatedAt = DateTime.UtcNow.ToLocalTime();

            await _sessionRepository.UpdateAsync(session);
            return true;
        }

        /// <summary>
        /// Updates the status of a session.
        /// - Only the assigned tutor can perform the update
        /// - Only sessions scheduled for TODAY can be updated by tutors
        /// - Only these 5 statuses are allowed: scheduled, processing, completed, rescheduled, cancelled
        /// </summary>
        public async Task<bool> UpdateSessionStatusAsync(Guid bookingId, string newStatus, Guid tutorId)
        {
            // 1. Load session
            var session = await _sessionRepository.GetByIdAsync(bookingId)
                          ?? throw new KeyNotFoundException("Session not found.");

            // 2. Permission check – must be the assigned tutor
            if (session.TutorId != tutorId)
                throw new UnauthorizedAccessException("You are not the tutor assigned to this session.");

            // 3. Critical rule – only TODAY's sessions can be updated by tutors
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (session.SessionDate != today)
            {
                throw new InvalidOperationException(
                    $"You can only update sessions scheduled for today ({today:dd/MM/yyyy}). " +
                    $"This session is on {session.SessionDate:dd/MM/yyyy}.");
            }

            // 4. Allowed statuses – strictly limited to these 5 values only
            var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "scheduled",
        "processing",
        "completed",
        "rescheduled",
        "cancelled"
    };

            var normalizedNewStatus = newStatus.Trim().ToLowerInvariant();

            if (!allowedStatuses.Contains(normalizedNewStatus))
            {
                throw new ArgumentException(
                    $"Invalid status '{newStatus}'. " +
                    $"Allowed values are: {string.Join(", ", allowedStatuses.OrderBy(s => s))}");
            }

            // 5. Prevent changing a final status (completed / cancelled / rescheduled)
            var currentStatus = session.Status?.Trim().ToLowerInvariant();
            var finalStatuses = new[] { "completed", "cancelled", "rescheduled" };

            if (finalStatuses.Contains(currentStatus) && normalizedNewStatus != currentStatus)
            {
                throw new InvalidOperationException(
                    $"Cannot change status from '{currentStatus}'. " +
                    "Sessions that are completed, cancelled or rescheduled cannot be modified.");
            }

            // 6. Apply update
            session.Status = normalizedNewStatus;
            session.UpdatedAt = DateTime.UtcNow.ToLocalTime();

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
        /// <summary>
        /// Thay tutor cho 1 buổi học cụ thể
        /// - Kiểm tra buổi học tồn tại
        /// - Kiểm tra tutor mới có rảnh không
        /// - Không cho đổi nếu buổi học đã completed hoặc cancelled
        /// </summary>
        public async Task<bool> ChangeSessionTutorAsync(ChangeSessionTutorRequest request, Guid staffId)
        {
            var session = await _sessionRepository.GetByIdAsync(request.BookingId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status is "completed" or "cancelled")
                throw new InvalidOperationException("Cannot change tutor for completed or cancelled session.");

            // Kiểm tra tutor mới có rảnh không
            var isAvailable = await _sessionRepository.IsTutorAvailableAsync(
                request.NewTutorId,
                session.SessionDate,
                session.StartTime,
                session.EndTime);

            if (!isAvailable)
                throw new InvalidOperationException(
                    $"Tutor is not available on {session.SessionDate:dd/MM/yyyy} " +
                    $"from {session.StartTime:HH:mm} to {session.EndTime:HH:mm}.");

            // Thực hiện thay đổi
            session.TutorId = request.NewTutorId;
            session.UpdatedAt = DateTime.UtcNow.ToLocalTime();

            await _sessionRepository.UpdateAsync(session);

            return true;
        }
        /// <summary>
        /// Lấy danh sách tutor có thể thay thế cho 1 buổi học cụ thể
        /// Ưu tiên: SubTutor trong Contract (nếu rảnh) → nếu không có → lấy tutor ngoài rảnh
        /// </summary>
        public async Task<object> GetReplacementTutorsAsync(Guid bookingId)
        {
            var session = await _sessionRepository.GetByIdAsync(bookingId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status is "completed" or "cancelled")
                throw new InvalidOperationException("Cannot replace tutor for completed or cancelled session.");

            var currentTutorId = session.TutorId;
            var contract = session.Contract;

            var result = new List<object>();
            var usedTutorIds = new HashSet<Guid> { currentTutorId };

            // 1. Ưu tiên SubTutor trong Contract
            var subTutorIds = new List<Guid?>
    {
        contract?.SubstituteTutor1Id,
        contract?.SubstituteTutor2Id
    }.Where(id => id.HasValue).Select(id => id.Value);

            foreach (var subId in subTutorIds.Where(id => id != currentTutorId))
            {
                var tutor = await _userRepository.GetByIdAsync(subId);
                if (tutor?.Status == "active")
                {
                    bool isAvailable = await _sessionRepository.IsTutorAvailableAsync(
                        subId, session.SessionDate, session.StartTime, session.EndTime);

                    if (isAvailable)
                    {
                        result.Add(new
                        {
                            tutorId = tutor.UserId,
                            fullName = tutor.FullName ?? "No name",
                            phoneNumber = tutor.PhoneNumber,
                            email = tutor.Email,
                            avatarUrl = tutor.AvatarUrl,
                            rating = tutor.FinalFeedbacks?.Any() == true
                                ? Math.Round(tutor.FinalFeedbacks.Average(f => f.OverallSatisfactionRating), 1)
                                : 0.0,
                            feedbackCount = tutor.FinalFeedbacks?.Count ?? 0,
                            isSubstitute = true,
                            priority = "high"
                        });
                        usedTutorIds.Add(subId);
                    }
                }
            }

            // 2. Nếu vẫn chưa có ai → lấy tutor ngoài
            if (!result.Any())
            {
                var allTutors = await _userRepository.GetTutorsAsync();
                foreach (var tutor in allTutors.Where(t => t.Status == "active" && !usedTutorIds.Contains(t.UserId)))
                {
                    bool isAvailable = await _sessionRepository.IsTutorAvailableAsync(
                        tutor.UserId, session.SessionDate, session.StartTime, session.EndTime);

                    if (isAvailable)
                    {
                        result.Add(new
                        {
                            tutorId = tutor.UserId,
                            fullName = tutor.FullName ?? "No name",
                            phoneNumber = tutor.PhoneNumber,
                            email = tutor.Email,
                            avatarUrl = tutor.AvatarUrl,
                            rating = tutor.FinalFeedbacks?.Any() == true
                                ? Math.Round(tutor.FinalFeedbacks.Average(f => f.OverallSatisfactionRating), 1)
                                : 0.0,
                            feedbackCount = tutor.FinalFeedbacks?.Count ?? 0,
                            isSubstitute = false,
                            priority = "normal"
                        });
                    }
                }
            }

            // Sắp xếp: SubTutor lên đầu, rồi mới đến tutor ngoài theo rating
            var sorted = result
                .OrderByDescending(x => (string)x.GetType().GetProperty("priority")!.GetValue(x) == "high")
                .ThenByDescending(x => (double)x.GetType().GetProperty("rating")!.GetValue(x))
                .ToList();

            return new
            {
                bookingId = session.BookingId,
                sessionDate = session.SessionDate.ToString("dd/MM/yyyy"),
                timeRange = $"{session.StartTime:HH:mm} - {session.EndTime:HH:mm}",
                childName = session.Contract?.Child?.FullName,
                currentTutor = session.Tutor?.FullName,
                replacementTutors = sorted,
                totalAvailable = sorted.Count,
                hasSubstitute = sorted.Any(x => (bool)x.GetType().GetProperty("isSubstitute")!.GetValue(x))
            };
        }
    }
}
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
        private readonly IChildRepository _childRepository;
        private readonly IContractRepository _contractRepository;

        public SessionService(ISessionRepository sessionRepository, IUserRepository userRepository, IChildRepository childRepository, IContractRepository contractRepository)
        {
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _childRepository = childRepository ?? throw new ArgumentNullException(nameof(childRepository));
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(childRepository));
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
            var mainChildName = s.Contract.Child?.FullName ?? "N/A";
            var secondChildName = s.Contract.SecondChild?.FullName;

            var studentNames = secondChildName != null
                ? $"{mainChildName} & {secondChildName}"
                : mainChildName;

            return new SessionDto
            {
                BookingId = s.BookingId,
                ContractId = s.ContractId,
                SessionDate = s.SessionDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                TutorName = s.Tutor?.FullName ?? "Teachers have not yet been assigned.",
                IsOnline = s.IsOnline,
                VideoCallPlatform = s.VideoCallPlatform,
                OfflineAddress = s.OfflineAddress,
                Status = s.Status,
                StudentNames = studentNames, 
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
        /// <summary>
        /// Lấy kế hoạch thay Main Tutor bị ban – CẬP NHẬT CẢ CONTRACT + TẤT CẢ BUỔI CÒN LẠI
        /// Không cần field UpdatedAt trong Contract
        /// </summary>
        public async Task<object> GetMainTutorReplacementPlanAsync(Guid contractId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                ?? throw new KeyNotFoundException("Contract not found.");

            if (contract.Status is "completed" or "cancelled")
                throw new InvalidOperationException("Contract is no longer active.");

            var mainTutor = await _userRepository.GetByIdAsync(contract.MainTutorId!.Value);
            //if (mainTutor == null || (mainTutor.Status != "banned" && mainTutor.Status != "inactive"))
            //    throw new InvalidOperationException("Only banned/inactive main tutor can be replaced.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcoming = await _sessionRepository.GetUpcomingSessionsByContractIdAsync(contractId, today);
            var sessionsToReplace = upcoming.Where(s => s.TutorId == contract.MainTutorId.Value).ToList();

            if (!sessionsToReplace.Any())
                throw new InvalidOperationException("No upcoming sessions for the main tutor.");

            var usedIds = new HashSet<Guid> { contract.MainTutorId.Value };
            if (contract.SubstituteTutor1Id.HasValue) usedIds.Add(contract.SubstituteTutor1Id.Value);
            if (contract.SubstituteTutor2Id.HasValue) usedIds.Add(contract.SubstituteTutor2Id.Value);

            var externalTutors = (await _userRepository.GetTutorsAsync())
                .Where(t => t.Status == "active" && !usedIds.Contains(t.UserId))
                .ToList();

            async Task<bool> IsFullyAvailable(Guid tutorId)
            {
                foreach (var s in sessionsToReplace)
                {
                    if (!await _sessionRepository.IsTutorAvailableAsync(tutorId, s.SessionDate, s.StartTime, s.EndTime))
                        return false;
                }
                return true;
            }

            // Tìm tutor rảnh nhất (ưu tiên SubTutor)
            User? bestMain = null;
            User? bestSub = null;

            // 1. Ưu tiên SubTutor 1
            if (contract.SubstituteTutor1Id.HasValue)
            {
                var sub1 = await _userRepository.GetByIdAsync(contract.SubstituteTutor1Id.Value);
                if (sub1?.Status == "active" && await IsFullyAvailable(sub1.UserId))
                {
                    bestMain = sub1;
                }
            }

            // 2. Nếu không có Sub1 → thử SubTutor 2
            if (bestMain == null && contract.SubstituteTutor2Id.HasValue)
            {
                var sub2 = await _userRepository.GetByIdAsync(contract.SubstituteTutor2Id.Value);
                if (sub2?.Status == "active" && await IsFullyAvailable(sub2.UserId))
                {
                    bestMain = sub2;
                }
            }

            // 3. Nếu không có Sub → lấy tutor ngoài
            if (bestMain == null)
            {
                bestMain = externalTutors.FirstOrDefault(t => IsFullyAvailable(t.UserId).Result);
            }

            // Tìm tutor bổ sung (Sub mới)
            if (bestMain != null)
            {
                var remainingTutors = externalTutors.Where(t => t.UserId != bestMain.UserId).ToList();
                bestSub = remainingTutors.FirstOrDefault(t => IsFullyAvailable(t.UserId).Result);
            }

            var recommendedPlan = bestMain == null ? null : new
            {
                planType = bestMain.UserId == contract.SubstituteTutor1Id || bestMain.UserId == contract.SubstituteTutor2Id
                    ? "promote_substitute"
                    : "external_replacement",
                newMainTutorId = bestMain.UserId,
                newMainTutorName = bestMain.FullName,
                newSubstituteTutorId = bestSub?.UserId,
                newSubstituteTutorName = bestSub?.FullName,
                ratingMain = GetRating(bestMain),
                ratingSub = bestSub != null ? GetRating(bestSub) : 0.0
            };

            return new
            {
                contractId,
                childName = contract.Child?.FullName,
                remainingSessions = sessionsToReplace.Count,
                bannedMainTutor = mainTutor.FullName,
                recommendedPlan,
                canProceed = recommendedPlan != null && bestSub != null,
                message = recommendedPlan == null ? "No replacement found." : "Ready to execute replacement."
            };
        }

        /// <summary>
        /// THỰC HIỆN thay Main Tutor – CẬP NHẬT CONTRACT + TẤT CẢ BUỔI CÒN LẠI
        /// Không cần UpdatedAt trong Contract
        /// </summary>
        public async Task<bool> ExecuteMainTutorReplacementAsync(Guid contractId, Guid newMainTutorId, Guid newSubstituteTutorId, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                ?? throw new KeyNotFoundException("Contract not found.");

            var newMain = await _userRepository.GetByIdAsync(newMainTutorId)
                ?? throw new KeyNotFoundException("New main tutor not found.");
            var newSub = await _userRepository.GetByIdAsync(newSubstituteTutorId)
                ?? throw new KeyNotFoundException("New substitute tutor not found.");

            if (newMain.Status != "active" || newSub.Status != "active")
                throw new InvalidOperationException("Both tutors must be active.");

            if (newMainTutorId == newSubstituteTutorId)
                throw new InvalidOperationException("Main tutor and substitute tutor must be different.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            var sessions = await _sessionRepository.GetUpcomingSessionsByContractIdAsync(contractId, today);
            var toUpdate = sessions.Where(s => s.TutorId == contract.MainTutorId.Value).ToList();

            if (!toUpdate.Any())
                throw new InvalidOperationException("No sessions to replace.");

            // Kiểm tra tutor mới có rảnh không
            foreach (var s in toUpdate)
            {
                var available = await _sessionRepository.IsTutorAvailableAsync(newMainTutorId, s.SessionDate, s.StartTime, s.EndTime);
                if (!available)
                    throw new InvalidOperationException($"Tutor {newMain.FullName} is not available on {s.SessionDate:dd/MM/yyyy}");
            }

            // Cập nhật tất cả buổi học
            foreach (var s in toUpdate)
            {
                s.TutorId = newMainTutorId;
                s.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            }
            await _sessionRepository.UpdateRangeAsync(toUpdate);

            // CẬP NHẬT CONTRACT – KHÔNG CẦN UpdatedAt
            contract.MainTutorId = newMainTutorId;

            // Đẩy tutor mới vào vị trí Sub trống
            if (contract.SubstituteTutor1Id == null || contract.SubstituteTutor1Id == contract.MainTutorId)
                contract.SubstituteTutor1Id = newSubstituteTutorId;
            else if (contract.SubstituteTutor2Id == null || contract.SubstituteTutor2Id == contract.MainTutorId)
                contract.SubstituteTutor2Id = newSubstituteTutorId;
            else
                contract.SubstituteTutor1Id = newSubstituteTutorId; // thay thế Sub1 nếu cả 2 đều đầy

            await _contractRepository.UpdateAsync(contract);

            return true;
        }

        // Helper – ĐÃ CÓ TRONG CLASS CỦA BẠN, GIỮ NGUYÊN HOẶC DÁN LẠI
        private double GetRating(User tutor)
        {
            return tutor.FinalFeedbacks?.Any() == true
                ? Math.Round(tutor.FinalFeedbacks.Average(f => f.OverallSatisfactionRating), 1)
                : 0.0;
        }
    }
}
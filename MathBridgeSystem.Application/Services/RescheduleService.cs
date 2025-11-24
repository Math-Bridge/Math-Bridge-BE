// MathBridgeSystem.Application.Services/RescheduleService.cs
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
    public class RescheduleService : IRescheduleService
    {
        private readonly IRescheduleRequestRepository _rescheduleRepo;
        private readonly IContractRepository _contractRepo;
        private readonly ISessionRepository _sessionRepo;
        private readonly IUserRepository _userRepo;
        private readonly IWalletTransactionRepository _walletTransactionRepo;

        // Valid start times: 16:00, 17:30, 19:00, 20:30
        private static readonly TimeOnly[] ValidStartTimes = new[]
         {
            new TimeOnly(16, 0),
            new TimeOnly(17, 30),
            new TimeOnly(19, 0),
            new TimeOnly(20, 30)
        };

        public RescheduleService(
             IRescheduleRequestRepository rescheduleRepo,
             IContractRepository contractRepo,
             ISessionRepository sessionRepo,
             IUserRepository userRepo,
             IWalletTransactionRepository walletTransactionRepo)
        {
            _rescheduleRepo = rescheduleRepo;
            _contractRepo = contractRepo;
            _sessionRepo = sessionRepo;
            _userRepo = userRepo;
            _walletTransactionRepo = walletTransactionRepo;
        }

        public async Task<RescheduleResponseDto> CreateRequestAsync(Guid parentId, CreateRescheduleRequestDto dto)
        {
            // 1. Validate time
            if (!IsValidStartTime(dto.StartTime))
                throw new ArgumentException("Start time must be 16:00, 17:30, 19:00, or 20:30.");

            var expectedEndTime = dto.StartTime.AddMinutes(90);
            if (dto.EndTime != expectedEndTime)
                throw new ArgumentException($"End time must be {expectedEndTime:HH:mm} (90 minutes after start time).");

            // 2. Validate session
            var oldSession = await _sessionRepo.GetByIdAsync(dto.BookingId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (oldSession.Contract.ParentId != parentId)
                throw new UnauthorizedAccessException("You can only reschedule your child's sessions.");

            if (oldSession.SessionDate < DateOnly.FromDateTime(DateTime.Today))
                throw new InvalidOperationException("Cannot reschedule past sessions.");

            // 3. ANTI-SPAM: Only one pending request per session
            var hasPendingInContract = await _rescheduleRepo.HasPendingRequestInContractAsync(oldSession.ContractId);
            if (hasPendingInContract)
            {
                throw new InvalidOperationException(
                    "This package already has one pending reschedule request. " +
                    "Only one reschedule request is allowed at a time per package. " +
                    "Please wait for the current request to be approved or rejected before submitting another.");
            }

            // 4. Load contract
            var contract = await _contractRepo.GetByIdWithPackageAsync(oldSession.ContractId)
                ?? throw new KeyNotFoundException("Contract not found.");

            if (contract.Status != "active")
                throw new InvalidOperationException("Contract is no longer active.");

            if (contract.EndDate < dto.RequestedDate)
                throw new InvalidOperationException("Requested date exceeds contract end date.");

            // 5. CRITICAL: Check remaining reschedule attempts
            if (contract.RescheduleCount <= 0)
                throw new InvalidOperationException(
                    "You have used all your reschedule attempts for this package. No more rescheduling is allowed.");

            // All checks passed → create request
            var request = new RescheduleRequest
            {
                RequestId = Guid.NewGuid(),
                BookingId = dto.BookingId,
                ContractId = oldSession.ContractId,
                ParentId = parentId,
                RequestedDate = dto.RequestedDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Reason = dto.Reason,
                Status = "pending",
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _rescheduleRepo.AddAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "pending",
                Message = "Reschedule request submitted successfully. Waiting for staff approval."
            };
        }

        public async Task<RescheduleResponseDto> ApproveRequestAsync(Guid staffId, Guid requestId, ApproveRescheduleRequestDto dto)
        {
            var request = await _rescheduleRepo.GetByIdWithDetailsAsync(requestId)
                ?? throw new KeyNotFoundException("Reschedule request not found.");

            if (request.Status != "pending")
                throw new InvalidOperationException("Only pending requests can be approved.");

            // Determine final tutor
            Guid finalTutorId = dto.NewTutorId != Guid.Empty
                ? dto.NewTutorId
                : request.RequestedTutorId ?? request.Booking.TutorId;

            var tutor = await _userRepo.GetByIdAsync(finalTutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");
            if (tutor.RoleId != 2)
                throw new InvalidOperationException("Selected user is not a tutor.");

            var startDt = request.RequestedDate.ToDateTime(request.StartTime);
            var endDt = request.RequestedDate.ToDateTime(request.EndTime);
            var isAvailable = await _sessionRepo.IsTutorAvailableAsync(finalTutorId, request.RequestedDate, startDt, endDt);
            if (!isAvailable)
                throw new InvalidOperationException("Selected tutor is not available at the requested time.");

            // Create new session
            var newSession = new Session
            {
                BookingId = Guid.NewGuid(),
                ContractId = request.ContractId,
                TutorId = finalTutorId,
                SessionDate = request.RequestedDate,
                StartTime = startDt,
                EndTime = endDt,
                IsOnline = request.Booking.IsOnline,
                VideoCallPlatform = request.Booking.VideoCallPlatform,
                OfflineAddress = request.Booking.OfflineAddress,
                OfflineLatitude = request.Booking.OfflineLatitude,
                OfflineLongitude = request.Booking.OfflineLongitude,
                Status = "scheduled",
                CreatedAt = DateTime.UtcNow.ToLocalTime()
            };

            await _sessionRepo.AddRangeAsync(new[] { newSession });

            // Mark old session as rescheduled
            request.Booking.Status = "rescheduled";
            request.Booking.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _sessionRepo.UpdateAsync(request.Booking);

            // Deduct one reschedule attempt (never go negative)
            var contract = request.Booking.Contract;
            contract.RescheduleCount = (byte)Math.Max(0, (contract.RescheduleCount ?? 0) - 1);
            contract.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            await _contractRepo.UpdateAsync(contract);

            // Finalize request
            request.Status = "approved";
            request.StaffId = staffId;
            request.RequestedTutorId = finalTutorId;
            request.ProcessedDate = DateTime.UtcNow.ToLocalTime();
            await _rescheduleRepo.UpdateAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "approved",
                Message = "Reschedule request approved successfully.",
                ProcessedDate = request.ProcessedDate
            };
        }

        public async Task<RescheduleResponseDto> RejectRequestAsync(Guid staffId, Guid requestId, string reason)
        {
            var request = await _rescheduleRepo.GetByIdWithDetailsAsync(requestId)
                ?? throw new KeyNotFoundException("Reschedule request not found.");

            if (request.Status != "pending")
                throw new InvalidOperationException("Only pending requests can be rejected.");

            request.Status = "rejected";
            request.StaffId = staffId;
            request.ProcessedDate = DateTime.UtcNow.ToLocalTime();
            request.Reason = reason;
            await _rescheduleRepo.UpdateAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "rejected",
                Message = $"Request rejected: {reason}",
                ProcessedDate = request.ProcessedDate
            };
        }
        public async Task<AvailableSubTutorsDto> GetAvailableSubTutorsAsync(Guid rescheduleRequestId)
        {
            // Get the reschedule request with contract details
            var rescheduleRequest = await _rescheduleRepo.GetByIdWithDetailsAsync(rescheduleRequestId);
            if (rescheduleRequest == null)
                throw new KeyNotFoundException("Reschedule request not found.");

            var contract = rescheduleRequest.Contract;
            if (contract == null)
                throw new KeyNotFoundException("Contract not found.");

            var availableTutors = new List<SubTutorInfoDto>();

            // Convert TimeOnly to DateTime for comparison
            var startDateTime = rescheduleRequest.RequestedDate.ToDateTime(rescheduleRequest.StartTime);
            var endDateTime = rescheduleRequest.RequestedDate.ToDateTime(rescheduleRequest.EndTime);

            // Check SubstituteTutor1
            if (contract.SubstituteTutor1Id.HasValue)
            {
                var isAvailable = await _sessionRepo.IsTutorAvailableAsync(
                    contract.SubstituteTutor1Id.Value,
                    rescheduleRequest.RequestedDate,
                    startDateTime,
                    endDateTime
                );

                if (isAvailable && contract.SubstituteTutor1 != null)
                {
                    var averageRating = contract.SubstituteTutor1.FinalFeedbacks.Any()
                        ? contract.SubstituteTutor1.FinalFeedbacks.Average(f => f.OverallSatisfactionRating)
                        : (double?)null;

                    availableTutors.Add(new SubTutorInfoDto
                    {
                        TutorId = contract.SubstituteTutor1.UserId,
                        FullName = contract.SubstituteTutor1.FullName,
                        PhoneNumber = contract.SubstituteTutor1.PhoneNumber,
                        Email = contract.SubstituteTutor1.Email,
                        Rating = averageRating,
                        IsAvailable = true
                    });
                }
            }

            // Check SubstituteTutor2
            if (contract.SubstituteTutor2Id.HasValue)
            {
                var isAvailable = await _sessionRepo.IsTutorAvailableAsync(
                    contract.SubstituteTutor2Id.Value,
                    rescheduleRequest.RequestedDate,
                    startDateTime,
                    endDateTime
                );

                if (isAvailable && contract.SubstituteTutor2 != null)
                {
                    var averageRating = contract.SubstituteTutor2.FinalFeedbacks.Any()
                        ? contract.SubstituteTutor2.FinalFeedbacks.Average(f => f.OverallSatisfactionRating)
                        : (double?)null;

                    availableTutors.Add(new SubTutorInfoDto
                    {
                        TutorId = contract.SubstituteTutor2.UserId,
                        FullName = contract.SubstituteTutor2.FullName,
                        PhoneNumber = contract.SubstituteTutor2.PhoneNumber,
                        Email = contract.SubstituteTutor2.Email,
                        Rating = averageRating,
                        IsAvailable = true
                    });
                }
            }

            return new AvailableSubTutorsDto
            {
                AvailableTutors = availableTutors,
                TotalAvailable = availableTutors.Count
            };
        }

        private bool IsValidStartTime(TimeOnly startTime)
        {
            foreach (var validTime in ValidStartTimes)
            {
                if (startTime == validTime)
                    return true;
            }
            return false;
        }

        public async Task<RescheduleRequestDto?> GetByIdAsync(Guid requestId, Guid userId, string role)
        {
            var request = await _rescheduleRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null)
                return null;

            // Authorization check
            if (role == "parent" && request.ParentId != userId)
                throw new UnauthorizedAccessException("You can only view your own reschedule requests.");

            return MapToDto(request);
        }

        public async Task<IEnumerable<RescheduleRequestDto>> GetAllAsync(Guid? parentId = null)
        {
            IEnumerable<RescheduleRequest> requests;

            if (parentId.HasValue)
            {
                requests = await _rescheduleRepo.GetByParentIdAsync(parentId.Value);
            }
            else
            {
                requests = await _rescheduleRepo.GetAllAsync();
            }

            return requests.Select(MapToDto);
        }

        private RescheduleRequestDto MapToDto(RescheduleRequest request)
        {
            return new RescheduleRequestDto
            {
                RequestId = request.RequestId,
                BookingId = request.BookingId,
                ParentId = request.ParentId,
                ParentName = request.Parent?.FullName ?? "N/A",
                ContractId = request.ContractId,
                RequestedDate = request.RequestedDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                RequestedTutorId = request.RequestedTutorId,
                RequestedTutorName = request.RequestedTutor?.FullName,
                Reason = request.Reason,
                Status = request.Status,
                StaffId = request.StaffId,
                StaffName = request.Staff?.FullName,
                ProcessedDate = request.ProcessedDate,
                CreatedDate = request.CreatedDate,
                OriginalSessionDate = request.Booking.SessionDate,
                OriginalStartTime = request.Booking.StartTime,
                OriginalEndTime = request.Booking.EndTime,
                OriginalTutorId = request.Booking.TutorId,
                OriginalTutorName = request.Booking.Tutor?.FullName ?? "N/A"
            };
        }

        public async Task<RescheduleResponseDto> CancelSessionAndRefundAsync(Guid sessionId, Guid rescheduleRequestId)
        {
            var rescheduleRequest = await _rescheduleRepo.GetByIdWithDetailsAsync(rescheduleRequestId)
                ?? throw new KeyNotFoundException("Reschedule request not found.");

            if (rescheduleRequest.BookingId != sessionId)
                throw new InvalidOperationException("Reschedule request does not belong to this session.");

            if (rescheduleRequest.Status != "pending")
                throw new InvalidOperationException($"Request is not pending (current: {rescheduleRequest.Status}).");

            var session = await _sessionRepo.GetByIdAsync(sessionId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status is "cancelled" or "completed")
                throw new InvalidOperationException("Session cannot be cancelled in its current state.");

            var contract = await _contractRepo.GetByIdWithPackageAsync(session.ContractId)
                ?? throw new KeyNotFoundException("Contract not found.");

            var refundAmount = contract.Package.Price / contract.Package.SessionCount;

            var transaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = contract.ParentId,
                ContractId = contract.ContractId,
                Amount = refundAmount,
                TransactionType = "Refund",
                Description = $"Refund for cancelled session on {session.SessionDate:dd/MM/yyyy} at {session.StartTime:HH:mm}",
                TransactionDate = DateTime.UtcNow.ToLocalTime(),
                Status = "Completed",
                PaymentMethod = "Wallet"
            };

            await _walletTransactionRepo.AddAsync(transaction);
            await _userRepo.UpdateWalletBalanceAsync(contract.ParentId, refundAmount);

            session.Status = "cancelled";
            session.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _sessionRepo.UpdateAsync(session);

            // Deduct attempt safely
            contract.RescheduleCount = (byte)Math.Max(0, (contract.RescheduleCount ?? 0) - 1);
            contract.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            await _contractRepo.UpdateAsync(contract);

            rescheduleRequest.Status = "approved";
            rescheduleRequest.ProcessedDate = DateTime.UtcNow.ToLocalTime();
            await _rescheduleRepo.UpdateAsync(rescheduleRequest);

            return new RescheduleResponseDto
            {
                RequestId = sessionId,
                Status = "cancelled",
                Message = $"Session cancelled successfully. Refund {refundAmount:N0} VND has been added to your wallet.",
                ProcessedDate = DateTime.UtcNow.ToLocalTime()
            };
        }
    }
}
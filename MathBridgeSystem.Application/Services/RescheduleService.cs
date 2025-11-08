// MathBridgeSystem.Application.Services/RescheduleService.cs
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class RescheduleService : IRescheduleService
    {
        private readonly IRescheduleRequestRepository _rescheduleRepo;
        private readonly IContractRepository _contractRepo;
        private readonly ISessionRepository _sessionRepo;

        public RescheduleService(
            IRescheduleRequestRepository rescheduleRepo,
            IContractRepository contractRepo,
            ISessionRepository sessionRepo)
        {
            _rescheduleRepo = rescheduleRepo;
            _contractRepo = contractRepo;
            _sessionRepo = sessionRepo;
        }

        public async Task<RescheduleResponseDto> CreateRequestAsync(Guid parentId, CreateRescheduleRequestDto dto)
        {
            var oldSession = await _sessionRepo.GetByIdAsync(dto.BookingId);
            if (oldSession == null) throw new KeyNotFoundException("Session not found.");
            if (oldSession.Contract.ParentId != parentId) throw new UnauthorizedAccessException("Not your child.");
            if (oldSession.SessionDate < DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("Cannot reschedule past sessions.");

            var hasPending = await _rescheduleRepo.HasPendingRequestForBookingAsync(dto.BookingId);
            if (hasPending) throw new InvalidOperationException("Pending request exists.");

            var contract = await _contractRepo.GetByIdWithPackageAsync(oldSession.ContractId);
            if (contract == null) throw new KeyNotFoundException("Contract not found.");
            if (contract.RescheduleCount >= contract.Package.MaxReschedule)
                throw new InvalidOperationException($"No reschedule attempts left. Max: {contract.Package.MaxReschedule}");

            var (start, end) = ParseTimeSlot(dto.RequestedTimeSlot);
            if (dto.RequestedTutorId.HasValue)
            {
                var startDateTime = dto.RequestedDate.ToDateTime(start);
                var endDateTime = dto.RequestedDate.ToDateTime(end);
                var available = await _sessionRepo.IsTutorAvailableAsync(dto.RequestedTutorId.Value, dto.RequestedDate, startDateTime, endDateTime);
                if (!available) throw new InvalidOperationException("Tutor not available.");
            }

            var request = new RescheduleRequest
            {
                RequestId = Guid.NewGuid(),
                BookingId = dto.BookingId,
                ContractId = oldSession.ContractId,
                ParentId = parentId,
                RequestedDate = dto.RequestedDate,
                RequestedTimeSlot = dto.RequestedTimeSlot,
                RequestedTutorId = dto.RequestedTutorId,
                Reason = dto.Reason,
                Status = "pending",
                CreatedDate = DateTime.UtcNow
            };

            await _rescheduleRepo.AddAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "pending",
                Message = "Yêu cầu đổi lịch đã được gửi."
            };
        }

        public async Task<RescheduleResponseDto> ApproveRequestAsync(Guid staffId, Guid requestId, ApproveRescheduleRequestDto dto)
        {
            var request = await _rescheduleRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null || request.Status != "pending") throw new KeyNotFoundException("Invalid request.");

            var (start, end) = ParseTimeSlot(request.RequestedTimeSlot);
            var finalTutorId = dto.NewTutorId != Guid.Empty ? dto.NewTutorId : (request.RequestedTutorId ?? request.Booking.TutorId);

            var startDateTime = request.RequestedDate.ToDateTime(start);
            var endDateTime = request.RequestedDate.ToDateTime(end);
            var available = await _sessionRepo.IsTutorAvailableAsync(finalTutorId, request.RequestedDate, startDateTime, endDateTime);
            if (!available) throw new InvalidOperationException("Tutor not available.");

            var sessionStart = request.RequestedDate.ToDateTime(start);
            var sessionEnd = request.RequestedDate.ToDateTime(end);

            var newSession = new Session
            {
                BookingId = Guid.NewGuid(),
                ContractId = request.ContractId,
                TutorId = finalTutorId,
                SessionDate = request.RequestedDate,
                StartTime = sessionStart,
                EndTime = sessionEnd,
                IsOnline = request.Booking.IsOnline,
                VideoCallPlatform = request.Booking.VideoCallPlatform,
                OfflineAddress = request.Booking.OfflineAddress,
                OfflineLatitude = request.Booking.OfflineLatitude,
                OfflineLongitude = request.Booking.OfflineLongitude,
                Status = "scheduled",
                CreatedAt = DateTime.UtcNow
            };

            await _sessionRepo.AddRangeAsync(new[] { newSession });

            request.Booking.Status = "rescheduled";
            request.Booking.UpdatedAt = DateTime.UtcNow;
            await _sessionRepo.UpdateAsync(request.Booking);

            var contract = request.Booking.Contract;
            contract.RescheduleCount += 1;
            contract.UpdatedDate = DateTime.UtcNow;
            await _contractRepo.UpdateAsync(contract);

            request.Status = "approved";
            request.StaffId = staffId;
            request.ProcessedDate = DateTime.UtcNow;
            await _rescheduleRepo.UpdateAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "approved",
                Message = "Đổi lịch thành công.",
                ProcessedDate = request.ProcessedDate
            };
        }

        public async Task<RescheduleResponseDto> RejectRequestAsync(Guid staffId, Guid requestId, string reason)
        {
            var request = await _rescheduleRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null || request.Status != "pending") throw new KeyNotFoundException("Invalid request.");

            request.Status = "rejected";
            request.StaffId = staffId;
            request.ProcessedDate = DateTime.UtcNow;
            request.Reason = reason;

            await _rescheduleRepo.UpdateAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "rejected",
                Message = $"Từ chối: {reason}",
                ProcessedDate = request.ProcessedDate
            };
        }

        private (TimeOnly start, TimeOnly end) ParseTimeSlot(string slot)
        {
            var parts = slot.Split('-');
            if (parts.Length != 2) throw new ArgumentException("Invalid time slot format. Use 'HH:mm-HH:mm'");
            return (TimeOnly.Parse(parts[0].Trim()), TimeOnly.Parse(parts[1].Trim()));
        }
    }
}
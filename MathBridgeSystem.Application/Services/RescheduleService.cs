﻿// MathBridgeSystem.Application.Services/RescheduleService.cs
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
        private readonly IUserRepository _userRepo;

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
            IUserRepository userRepo)
        {
            _rescheduleRepo = rescheduleRepo;
            _contractRepo = contractRepo;
            _sessionRepo = sessionRepo;
            _userRepo = userRepo;
        }

        public async Task<RescheduleResponseDto> CreateRequestAsync(Guid parentId, CreateRescheduleRequestDto dto)
        {
            // Validate start time
            if (!IsValidStartTime(dto.StartTime))
            {
                throw new ArgumentException("Start time must be 16:00, 17:30, 19:00, or 20:30.");
            }

            // Validate end time (must be 90 minutes from start time)
            var expectedEndTime = dto.StartTime.AddMinutes(90);
            if (dto.EndTime != expectedEndTime)
            {
                throw new ArgumentException($"End time must be 90 minutes after start time. Expected: {expectedEndTime}");
            }

            var oldSession = await _sessionRepo.GetByIdAsync(dto.BookingId);
            if (oldSession == null) throw new KeyNotFoundException("Session not found.");
            if (oldSession.Contract.ParentId != parentId) throw new UnauthorizedAccessException("Not your child.");
            if (oldSession.SessionDate < DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime())) throw new InvalidOperationException("Cannot reschedule past sessions.");

            var hasPending = await _rescheduleRepo.HasPendingRequestForBookingAsync(dto.BookingId);
            if (hasPending) throw new InvalidOperationException("Pending request exists.");

            var contract = await _contractRepo.GetByIdWithPackageAsync(oldSession.ContractId);
            if (contract == null) throw new KeyNotFoundException("Contract not found.");
            if (contract.Status != "active")
                throw new InvalidOperationException("Contract is not active.");
            if(contract.EndDate < dto.RequestedDate)
                throw new InvalidOperationException("Requested date exceeds contract end date.");
            if (contract.RescheduleCount >= contract.Package.MaxReschedule)
                throw new InvalidOperationException($"No reschedule attempts left. Max: {contract.Package.MaxReschedule}");
            

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
                Message = "Yêu cầu đổi lịch đã được gửi."
            };
        }

        public async Task<RescheduleResponseDto> ApproveRequestAsync(Guid staffId, Guid requestId, ApproveRescheduleRequestDto dto)
        {
            var request = await _rescheduleRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null || request.Status != "pending") throw new KeyNotFoundException("Invalid request.");

            // Determine the final tutor ID
            Guid finalTutorId;
            if (dto.NewTutorId != Guid.Empty)
            {
                // Validate that the new tutor exists and is a tutor
                var newTutor = await _userRepo.GetByIdAsync(dto.NewTutorId);
                if (newTutor == null)
                    throw new KeyNotFoundException($"Tutor with ID {dto.NewTutorId} not found.");
                if (newTutor.RoleId != 2) // 2 = tutor role
                    throw new InvalidOperationException($"User {dto.NewTutorId} is not a tutor.");
                finalTutorId = dto.NewTutorId;
            }
            else if (request.RequestedTutorId.HasValue)
            {
                // Validate that the requested tutor exists and is a tutor
                var requestedTutor = await _userRepo.GetByIdAsync(request.RequestedTutorId.Value);
                if (requestedTutor == null)
                    throw new KeyNotFoundException($"Requested tutor with ID {request.RequestedTutorId} not found.");
                if (requestedTutor.RoleId != 2)
                    throw new InvalidOperationException($"Requested user {request.RequestedTutorId} is not a tutor.");
                finalTutorId = request.RequestedTutorId.Value;
            }
            else
            {
                // Use the original tutor
                finalTutorId = request.Booking.TutorId;
            }

            var startDateTime = request.RequestedDate.ToDateTime(request.StartTime);
            var endDateTime = request.RequestedDate.ToDateTime(request.EndTime);
            var available = await _sessionRepo.IsTutorAvailableAsync(finalTutorId, request.RequestedDate, startDateTime, endDateTime);
            if (!available) throw new InvalidOperationException("Tutor not available.");

            var sessionStart = request.RequestedDate.ToDateTime(request.StartTime);
            var sessionEnd = request.RequestedDate.ToDateTime(request.EndTime);

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
                CreatedAt = DateTime.UtcNow.ToLocalTime()
            };

            await _sessionRepo.AddRangeAsync(new[] { newSession });

            request.Booking.Status = "rescheduled";
            request.Booking.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _sessionRepo.UpdateAsync(request.Booking);

            var contract = request.Booking.Contract;
            contract.RescheduleCount += 1;
            contract.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            await _contractRepo.UpdateAsync(contract);

            request.Status = "approved";
            request.StaffId = staffId;
            request.RequestedTutorId = finalTutorId;
            request.ProcessedDate = DateTime.UtcNow.ToLocalTime();
            await _rescheduleRepo.UpdateAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "approved",
                Message = "Done reschedule",
                ProcessedDate = request.ProcessedDate
            };
        }

        public async Task<RescheduleResponseDto> RejectRequestAsync(Guid staffId, Guid requestId, string reason)
        {
            var request = await _rescheduleRepo.GetByIdWithDetailsAsync(requestId);
            if (request == null || request.Status != "pending") throw new KeyNotFoundException("Invalid request.");

            request.Status = "rejected";
            request.StaffId = staffId;
            request.ProcessedDate = DateTime.UtcNow.ToLocalTime();
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
    }
}
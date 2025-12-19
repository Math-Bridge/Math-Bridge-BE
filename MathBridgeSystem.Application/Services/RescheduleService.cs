// MathBridgeSystem.Application.Services/RescheduleService.cs
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Org.BouncyCastle.Tls;
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
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

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
             IWalletTransactionRepository walletTransactionRepo,
             IEmailService emailService,
             INotificationService notificationService)
        {
            _rescheduleRepo = rescheduleRepo;
            _contractRepo = contractRepo;
            _sessionRepo = sessionRepo;
            _userRepo = userRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _emailService = emailService;
            _notificationService = notificationService;
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
                RequestedTutorId = oldSession.TutorId,
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _rescheduleRepo.AddAsync(request);

            // Send notification and email to parent
            var parent = await _userRepo.GetByIdAsync(parentId);
            var childName = contract.Child?.FullName ?? "your child";

            // Create notification
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = parentId,
                ContractId = contract.ContractId,
                BookingId = dto.BookingId,
                Title = "Reschedule Request Submitted",
                Message = $"Your reschedule request for {childName}'s session on {dto.RequestedDate:dd/MM/yyyy} at {dto.StartTime:HH:mm} has been submitted. Please wait for staff approval.",
                NotificationType = "Reschedule"
            });

            // Send email
            if (parent != null && !string.IsNullOrEmpty(parent.Email))
            {
                try
                {
                    await _emailService.SendRescheduleRequestCreatedAsync(
                        parent.Email,
                        parent.FullName ?? "Parent",
                        childName,
                        oldSession.SessionDate.ToString("dd/MM/yyyy"),
                        $"{TimeOnly.FromDateTime(oldSession.StartTime):HH:mm} - {TimeOnly.FromDateTime(oldSession.EndTime):HH:mm}",
                        dto.RequestedDate.ToString("dd/MM/yyyy"),
                        $"{dto.StartTime:HH:mm} - {dto.EndTime:HH:mm}",
                        dto.Reason ?? "No reason provided"
                    );
                }
                catch
                {
                    // Log but don't fail the request if email fails
                }
            }

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

            // PHÁT HIỆN LOẠI YÊU CẦU
            bool isTutorReplacement = request.Reason?.Contains("[CHANGE TUTOR]", StringComparison.OrdinalIgnoreCase) == true;

            Guid finalTutorId;

            // Nếu Staff truyền newTutorId → dùng luôn
            if (dto.NewTutorId.HasValue && dto.NewTutorId != Guid.Empty)
            {
                finalTutorId = dto.NewTutorId.Value;
            }
            // Các trường hợp khác → giữ nguyên tutor cũ
            else
            {
                finalTutorId = request.Booking.TutorId;
            }

            // Kiểm tra tutor hợp lệ
            var tutor = await _userRepo.GetByIdAsync(finalTutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            if (tutor.RoleId != 2)
                throw new InvalidOperationException("Selected user is not a tutor.");

            // BỎ KIỂM TRA RẢNH CHO YÊU CẦU THAY TUTOR (vì tutor bận nên mới gửi request)
            if (!isTutorReplacement)
            {
                var startDt = request.RequestedDate.ToDateTime(request.StartTime);
                var endDt = request.RequestedDate.ToDateTime(request.EndTime);

                var isAvailable = await _sessionRepo.IsTutorAvailableAsync(finalTutorId, request.RequestedDate, startDt, endDt);
                if (!isAvailable)
                    throw new InvalidOperationException("Selected tutor is not available at the requested time.");
            }

            // Tạo buổi học mới (nếu cần – ở đây vẫn tạo để giữ logic cũ, hoặc bạn có thể bỏ nếu không muốn tạo buổi mới)
            var newSession = new Session
            {
                BookingId = Guid.NewGuid(),
                ContractId = request.ContractId,
                TutorId = finalTutorId,
                SessionDate = request.RequestedDate,
                StartTime = request.RequestedDate.ToDateTime(request.StartTime),
                EndTime = request.RequestedDate.ToDateTime(request.EndTime),
                IsOnline = request.Booking.IsOnline,
                VideoCallPlatform = request.Booking.VideoCallPlatform,
                OfflineAddress = request.Booking.OfflineAddress,
                OfflineLatitude = request.Booking.OfflineLatitude,
                OfflineLongitude = request.Booking.OfflineLongitude,
                Status = "scheduled",
                CreatedAt = DateTime.UtcNow.ToLocalTime()
            };

            await _sessionRepo.AddRangeAsync(new[] { newSession });

            // NẾU LÀ YÊU CẦU THAY TUTOR → BUỔI CŨ CHUYỂN THÀNH "cancelled"
            if (isTutorReplacement)
            {
                request.Booking.Status = "cancelled"; // cancelled thay vì rescheduled
            }
            else
            {
                request.Booking.Status = "rescheduled"; // Các loại khác giữ nguyên
            }

            request.Booking.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _sessionRepo.UpdateAsync(request.Booking);

            // CHỈ TRỪ RescheduleCount CHO DỜI LỊCH BÌNH THƯỜNG
            bool isNormalReschedule = !isTutorReplacement &&
                                      request.Reason?.Contains("Reschedule due to Tutor unavailability") != true;

            if (isNormalReschedule)
            {
                var contract = request.Booking.Contract;
                contract.RescheduleCount = (byte)Math.Max(0, (contract.RescheduleCount ?? 0) - 1);
                contract.UpdatedDate = DateTime.UtcNow.ToLocalTime();
                await _contractRepo.UpdateAsync(contract);
            }

            // Lưu note
            if (!string.IsNullOrEmpty(dto.Note))
            {
                request.Reason += $" | Staff note: {dto.Note}";
            }

            // Hoàn tất request
            request.Status = "approved";
            request.StaffId = staffId;
            request.RequestedTutorId = finalTutorId;
            request.ProcessedDate = DateTime.UtcNow.ToLocalTime();
            await _rescheduleRepo.UpdateAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "approved",
                Message = "Request approved successfully.",
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

            // Send notification and email to parent
            var parent = await _userRepo.GetByIdAsync(request.ParentId);
            var childName = request.Contract?.Child?.FullName ?? "your child";

            // Create notification
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = request.ParentId,
                ContractId = request.ContractId,
                BookingId = request.BookingId,
                Title = "Reschedule Request Rejected",
                Message = $"Your reschedule request for {childName}'s session has been rejected. Reason: {reason}. The original session remains unchanged.",
                NotificationType = "Reschedule"
            });

            // Send email
            if (parent != null && !string.IsNullOrEmpty(parent.Email))
            {
                try
                {
                    await _emailService.SendRescheduleRejectedAsync(
                        parent.Email,
                        parent.FullName ?? "Parent",
                        childName,
                        request.Booking.SessionDate.ToString("dd/MM/yyyy"),
                        $"{TimeOnly.FromDateTime(request.Booking.StartTime):HH:mm} - {TimeOnly.FromDateTime(request.Booking.EndTime):HH:mm}",
                        reason
                    );
                }
                catch
                {
                    // Log but don't fail the request if email fails
                }
            }

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

                if (isAvailable && contract.SubstituteTutor1 != null && contract.SubstituteTutor1.Status != "banned" )
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
            if (contract.SubstituteTutor2Id.HasValue )
            {
                var isAvailable = await _sessionRepo.IsTutorAvailableAsync(
                    contract.SubstituteTutor2Id.Value,
                    rescheduleRequest.RequestedDate,
                    startDateTime,
                    endDateTime
                );

                if (isAvailable && contract.SubstituteTutor2 != null && contract.SubstituteTutor2.Status != "banned")
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
                ChildId = request.Contract.ChildId,
                ChildName = request.Contract.Child?.FullName ?? "N/A",
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

            if (rescheduleRequest.Status != "approved" && rescheduleRequest.Status != "pending")
                throw new InvalidOperationException($"Request is not pending or approved (current: {rescheduleRequest.Status}).");

            var session = await _sessionRepo.GetByIdAsync(sessionId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status is "cancelled" or "completed")
                throw new InvalidOperationException("Session cannot be cancelled in its current state.");

            var contract = await _contractRepo.GetByIdWithPackageAsync(session.ContractId)
                ?? throw new KeyNotFoundException("Contract not found.");

            // SỬA CHÍNH TẠI ĐÂY: TÍNH GIÁ HOÀN TIỀN ĐÚNG CHO CẢ CONTRACT THƯỜNG VÀ TWIN
            decimal basePrice = contract.Package.Price;
            int sessionCount = contract.Package.SessionCount;

            // Nếu là contract twin → giá thực thu = basePrice * 1.6 nghĩa là 160%
            bool isTwinContract = contract.SecondChildId.HasValue;
            decimal actualContractPrice = isTwinContract ? basePrice * 1.6m : basePrice;

            decimal refundAmount = actualContractPrice / sessionCount;

            var transaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = contract.ParentId,
                ContractId = contract.ContractId,
                Amount = refundAmount,
                TransactionType = "Refund",
                Description = $"Refund for cancelled session on {session.SessionDate:dd/MM/yyyy} at {session.StartTime:HH:mm}" +
                              (isTwinContract ? " (Twin contract)" : ""),
                TransactionDate = DateTime.UtcNow.ToLocalTime(),
                Status = "Completed",
                PaymentMethod = "Wallet"
            };

            await _walletTransactionRepo.AddAsync(transaction);
            await _userRepo.UpdateWalletBalanceAsync(contract.ParentId, refundAmount);

            session.Status = "cancelled";
            session.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _sessionRepo.UpdateAsync(session);

            // Trừ lượt reschedule nếu là dời lịch từ phụ huynh
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
        public async Task<object> CreateTutorReplacementRequestAsync(Guid bookingId, Guid tutorId, string reason)
        {
            var session = await _sessionRepo.GetByIdAsync(bookingId)
                ?? throw new KeyNotFoundException("No lesson found.");

            if (session.TutorId != tutorId)
                throw new UnauthorizedAccessException("You can only submit requests for your own lessons.");

            if (session.SessionDate <= DateOnly.FromDateTime(DateTime.Today))
                throw new InvalidOperationException("Replacements are only permitted for appointments starting tomorrow.");

            if (session.Status != "scheduled")
                throw new InvalidOperationException("The lesson has been canceled or completed.");

            // Không gửi trùng
            var hasPending = await _rescheduleRepo.HasPendingRequestForBookingAsync(bookingId);
            if (hasPending)
                throw new InvalidOperationException("There are pending requests for this lesson.");

            var rescheduleRequest = new RescheduleRequest
            {
                RequestId = Guid.NewGuid(),
                BookingId = bookingId,
                ContractId = session.ContractId,
                ParentId = session.Contract.ParentId,
                RequestedDate = session.SessionDate,
                StartTime = TimeOnly.FromDateTime(session.StartTime),
                EndTime = TimeOnly.FromDateTime(session.EndTime),
                Reason = $"[CHANGE TUTOR] {reason}",
                Status = "pending",
                RequestedTutorId = session.TutorId,
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _rescheduleRepo.AddAsync(rescheduleRequest);
            return new
            {
                requestId = rescheduleRequest.RequestId,
                bookingId,
                sessionDate = session.SessionDate,
                originalTutor = session.Tutor.FullName,
                message = "The request to replace the tutor has been successfully submitted. Please wait for staff to process it."
            };
        }
        public async Task<RescheduleResponseDto> CreateMakeUpSessionRequestAsync(Guid parentId, CreateRescheduleRequestDto dto)
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
                throw new UnauthorizedAccessException("You can only request make-up for your child's sessions.");

            if (oldSession.SessionDate < DateOnly.FromDateTime(DateTime.Today))
                throw new InvalidOperationException("Cannot request make-up for past sessions.");

            // 3. KIỂM TRA CÓ REQUEST THAY TUTOR ĐÃ APPROVED CHƯA
            var allRequests = await _rescheduleRepo.GetAllAsync();

            var tutorReplacementRequest = allRequests
                .FirstOrDefault(r => r.BookingId == dto.BookingId &&
                                     r.Reason != null &&
                                     r.Reason.Contains("[CHANGE TUTOR]", StringComparison.OrdinalIgnoreCase));

            if (tutorReplacementRequest != null)
            {
                if (tutorReplacementRequest.Status != "approved")
                {
                    throw new InvalidOperationException(
                                        "Tutor has submitted a replacement request for this lesson and is awaiting staff processing." +
                                            "Please wait for staff approval before requesting a make-up lesson.");
                }

                // ĐÃ APPROVED → CHO PHÉP TẠO MAKE-UP NHƯNG KHÔNG ĐƯỢC CHỌN NGÀY TRÙNG VỚI NGÀY TUTOR BẬN
                if (dto.RequestedDate == oldSession.SessionDate)
                {
                    throw new InvalidOperationException(
                    "Cannot select a make-up date that coincides with the tutor's busy date(" + oldSession.SessionDate.ToString("dd/MM/yyyy") + "). " +
                    "Please select a different make-up date.");
                }
            }

            // 4. ANTI-SPAM: Không cho tạo nhiều make-up request cùng lúc
            var hasPendingMakeUp = await _rescheduleRepo.HasPendingRequestForBookingAsync(dto.BookingId);
            if (hasPendingMakeUp)
                throw new InvalidOperationException("There is already a pending make-up request for this session.");

            // 5. Load contract
            var contract = await _contractRepo.GetByIdWithPackageAsync(oldSession.ContractId)
                ?? throw new KeyNotFoundException("Contract not found.");

            if (contract.Status != "active")
                throw new InvalidOperationException("Contract is no longer active.");

            if (contract.EndDate < dto.RequestedDate)
                throw new InvalidOperationException("Requested make-up date exceeds contract end date.");

            // Tạo request dạy bù
            string defaultReason = "Reschedule due to Tutor unavailability";

            var request = new RescheduleRequest
            {
                RequestId = Guid.NewGuid(),
                BookingId = dto.BookingId,
                ContractId = oldSession.ContractId,
                ParentId = parentId,
                RequestedDate = dto.RequestedDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Reason = defaultReason,
                Status = "pending",
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _rescheduleRepo.AddAsync(request);

            return new RescheduleResponseDto
            {
                RequestId = request.RequestId,
                Status = "pending",
                Message = "Make-up session request submitted successfully. Staff will arrange the new session soon. (This does not count against your reschedule attempts.)"
            };
        }
    }
}
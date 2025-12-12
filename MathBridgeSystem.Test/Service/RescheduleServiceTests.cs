﻿using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace MathBridgeSystem.Tests.Services
{
    public class RescheduleServiceTests
    {
        private readonly Mock<IRescheduleRequestRepository> _rescheduleRepoMock;
        private readonly Mock<IContractRepository> _contractRepoMock;
        private readonly Mock<ISessionRepository> _sessionRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IWalletTransactionRepository> _walletRepoMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly RescheduleService _rescheduleService;

        private readonly Guid _parentId = Guid.NewGuid();
        private readonly Guid _staffId = Guid.NewGuid();
        private readonly Guid _tutorId = Guid.NewGuid();
        private readonly Guid _contractId = Guid.NewGuid();
        private readonly Guid _bookingId = Guid.NewGuid();
        private readonly PaymentPackage _package;
        private readonly Contract _contract;
        private readonly Session _session;

        public RescheduleServiceTests()
        {
            _rescheduleRepoMock = new Mock<IRescheduleRequestRepository>();
            _contractRepoMock = new Mock<IContractRepository>();
            _sessionRepoMock = new Mock<ISessionRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _walletRepoMock = new Mock<IWalletTransactionRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _notificationServiceMock = new Mock<INotificationService>();

            _rescheduleService = new RescheduleService(
                _rescheduleRepoMock.Object,
                _contractRepoMock.Object,
                _sessionRepoMock.Object,
                _userRepoMock.Object,
                _walletRepoMock.Object,
                _emailServiceMock.Object,
                _notificationServiceMock.Object
            );

            // --- Khởi tạo dữ liệu Mock chung ---
            _package = new PaymentPackage { PackageId = Guid.NewGuid(), MaxReschedule = 2 };

            _contract = new Contract
            {
                ContractId = _contractId,
                ParentId = _parentId,
                RescheduleCount = 2,
                Package = _package,
                Status = "active",
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(30))
            };

            _session = new Session
            {
                BookingId = _bookingId,
                ContractId = _contractId,
                Contract = _contract, 
                SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(5)), 
                TutorId = _tutorId,
                Status = "scheduled"
            };
        }


        // Test: Tạo request thành công (không yêu cầu tutor cụ thể)
        [Fact]
        public async Task CreateRequestAsync_ValidRequest_CreatesRequest()
        {
            // Arrange
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = _bookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30),
                Reason = "Bận"
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(_session);
            _rescheduleRepoMock.Setup(r => r.HasPendingRequestInContractAsync(_contractId)).ReturnsAsync(false);
            _contractRepoMock.Setup(r => r.GetByIdWithPackageAsync(_contractId)).ReturnsAsync(_contract);
            _rescheduleRepoMock.Setup(r => r.AddAsync(It.IsAny<RescheduleRequest>())).Returns(Task.CompletedTask);

            // Act
            var result = await _rescheduleService.CreateRequestAsync(_parentId, dto);

            // Assert
            result.Status.Should().Be("pending");
            result.Message.Should().Be("Reschedule request submitted successfully. Waiting for staff approval.");
            _rescheduleRepoMock.Verify(r => r.AddAsync(It.IsAny<RescheduleRequest>()), Times.Once);
        }

        // Test: Tạo request thành công (có yêu cầu tutor và tutor rảnh)
        [Fact]
        public async Task CreateRequestAsync_ValidRequestWithTutor_ChecksAvailabilityAndCreatesRequest()
        {
            // Arrange
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = _bookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)),
                StartTime = new TimeOnly(17, 30),
                EndTime = new TimeOnly(19, 0)
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(_session);
            _rescheduleRepoMock.Setup(r => r.HasPendingRequestInContractAsync(_contractId)).ReturnsAsync(false);
            _contractRepoMock.Setup(r => r.GetByIdWithPackageAsync(_contractId)).ReturnsAsync(_contract);

            // Act
            var result = await _rescheduleService.CreateRequestAsync(_parentId, dto);

            // Assert
            result.Status.Should().Be("pending");
            _rescheduleRepoMock.Verify(r => r.AddAsync(It.IsAny<RescheduleRequest>()), Times.Once);
        }

        // Test: Ném lỗi khi không tìm thấy Session
        [Fact]
        public async Task CreateRequestAsync_SessionNotFound_ThrowsKeyNotFoundException()
        {
            _sessionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Session)null);
            var dto = new CreateRescheduleRequestDto { BookingId = _bookingId, RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)), StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 30) };
            Func<Task> act = () => _rescheduleService.CreateRequestAsync(_parentId, dto);
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Session not found.");
        }

        // Test: Ném lỗi khi không đúng Parent
        [Fact]
        public async Task CreateRequestAsync_NotYourChild_ThrowsUnauthorizedAccessException()
        {
            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(_session);
            var otherParentId = Guid.NewGuid();
            var dto = new CreateRescheduleRequestDto { BookingId = _bookingId, RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)), StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 30) };
            Func<Task> act = () => _rescheduleService.CreateRequestAsync(otherParentId, dto);
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("You can only reschedule your child's sessions.");
        }

        // Test: Ném lỗi khi đổi lịch buổi học trong quá khứ
        [Fact]
        public async Task CreateRequestAsync_PastSession_ThrowsInvalidOperationException()
        {
            var pastSession = new Session { Contract = _contract, SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };
            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(pastSession);
            var dto = new CreateRescheduleRequestDto { BookingId = _bookingId, RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)), StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 30) };
            Func<Task> act = () => _rescheduleService.CreateRequestAsync(_parentId, dto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot reschedule past sessions.");
        }

        // Test: Ném lỗi khi đã có request đang chờ trong contract
        [Fact]
        public async Task CreateRequestAsync_PendingRequestExists_ThrowsInvalidOperationException()
        {
            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(_session);
            _rescheduleRepoMock.Setup(r => r.HasPendingRequestInContractAsync(_contractId)).ReturnsAsync(true);
            var dto = new CreateRescheduleRequestDto { BookingId = _bookingId, RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)), StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 30) };
            Func<Task> act = () => _rescheduleService.CreateRequestAsync(_parentId, dto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("This package already has one pending reschedule request. " +
                    "Only one reschedule request is allowed at a time per package. " +
                    "Please wait for the current request to be approved or rejected before submitting another.");
        }

        // Test: Ném lỗi khi không tìm thấy Contract
        [Fact]
        public async Task CreateRequestAsync_ContractNotFound_ThrowsKeyNotFoundException()
        {
            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(_session);
            _rescheduleRepoMock.Setup(r => r.HasPendingRequestInContractAsync(_contractId)).ReturnsAsync(false);
            _contractRepoMock.Setup(r => r.GetByIdWithPackageAsync(_contractId)).ReturnsAsync((Contract)null);
            var dto = new CreateRescheduleRequestDto { BookingId = _bookingId, RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)), StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 30) };
            Func<Task> act = () => _rescheduleService.CreateRequestAsync(_parentId, dto);
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Contract not found.");
        }

        // Test: Ném lỗi khi hết lượt đổi lịch
        [Fact]
        public async Task CreateRequestAsync_NoRescheduleAttemptsLeft_ThrowsInvalidOperationException()
        {
            _contract.RescheduleCount = 0;  // No attempts left
            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(_session);
            _rescheduleRepoMock.Setup(r => r.HasPendingRequestInContractAsync(_contractId)).ReturnsAsync(false);
            _contractRepoMock.Setup(r => r.GetByIdWithPackageAsync(_contractId)).ReturnsAsync(_contract);
            var dto = new CreateRescheduleRequestDto { BookingId = _bookingId, RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)), StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 30) };
            Func<Task> act = () => _rescheduleService.CreateRequestAsync(_parentId, dto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You have used all your reschedule attempts for this package. No more rescheduling is allowed.");
        }

        // Test: Khi yêu cầu tutor không rảnh hiện tại, service vẫn tạo request (service không kiểm tra requested tutor trong CreateRequestAsync)
        [Fact]
        public async Task CreateRequestAsync_TutorNotAvailable_ServiceCreatesRequest()
        {
            var dto = new CreateRescheduleRequestDto { BookingId = _bookingId, RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)), StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(20, 30) };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(_bookingId)).ReturnsAsync(_session);
            _rescheduleRepoMock.Setup(r => r.HasPendingRequestInContractAsync(_contractId)).ReturnsAsync(false);
            _contractRepoMock.Setup(r => r.GetByIdWithPackageAsync(_contractId)).ReturnsAsync(_contract);
            _rescheduleRepoMock.Setup(r => r.AddAsync(It.IsAny<RescheduleRequest>())).Returns(Task.CompletedTask);

            var result = await _rescheduleService.CreateRequestAsync(_parentId, dto);

            result.Status.Should().Be("pending");
            _rescheduleRepoMock.Verify(r => r.AddAsync(It.IsAny<RescheduleRequest>()), Times.Once);
        }

        // Test: Ném lỗi khi thời gian bắt đầu không hợp lệ
        [Fact]
        public async Task CreateRequestAsync_InvalidStartTime_ThrowsArgumentException()
        {
            var dto = new CreateRescheduleRequestDto 
            { 
                BookingId = _bookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)),
                StartTime = new TimeOnly(10, 0), // Invalid start time
                EndTime = new TimeOnly(11, 30)
            }; 

            Func<Task> act = () => _rescheduleService.CreateRequestAsync(_parentId, dto);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Start time must be 16:00, 17:30, 19:00, or 20:30.");
        }

        // Test: Ném lỗi khi thời gian kết thúc không phải 90 phút từ thời gian bắt đầu
        [Fact]
        public async Task CreateRequestAsync_InvalidEndTime_ThrowsArgumentException()
        {
            var dto = new CreateRescheduleRequestDto 
            { 
                BookingId = _bookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime().AddDays(10)),
                StartTime = new TimeOnly(16, 0), // Valid start time
                EndTime = new TimeOnly(18, 0) // Invalid: should be 17:30 (90 mins later)
            }; 

            Func<Task> act = () => _rescheduleService.CreateRequestAsync(_parentId, dto);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("End time must be 17:30 (90 minutes after start time).");
        }


        // Test: Duyệt request thành công
        [Fact]
        public async Task ApproveRequestAsync_ValidRequest_ApprovesAndCreatesNewSession()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var newTutorId = Guid.NewGuid();
            var dto = new ApproveRescheduleRequestDto { NewTutorId = newTutorId };

            var newTutor = new User
            {
                UserId = newTutorId,
                RoleId = 2, // Tutor role
                Email = "tutor@example.com",
                FullName = "Test Tutor"
            };

            var request = new RescheduleRequest
            {
                RequestId = requestId,
                Status = "pending",
                Booking = _session, 
                ContractId = _contractId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                StartTime = new TimeOnly(20, 30),
                EndTime = new TimeOnly(22, 0),
                RequestedTutorId = null
            };

            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _userRepoMock.Setup(u => u.GetByIdAsync(newTutorId)).ReturnsAsync(newTutor);
            _sessionRepoMock.Setup(r => r.IsTutorAvailableAsync(newTutorId, request.RequestedDate, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(true);

            // Act
            var result = await _rescheduleService.ApproveRequestAsync(_staffId, requestId, dto);

            // Assert
            result.Status.Should().Be("approved");
            result.Message.Should().Be("Reschedule request approved successfully.");

            _sessionRepoMock.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Session>>(list =>
                list.Count() == 1 &&
                list.First().TutorId == newTutorId &&
                list.First().Status == "scheduled")),
                Times.Once);

            _session.Status.Should().Be("rescheduled");
            _sessionRepoMock.Verify(r => r.UpdateAsync(_session), Times.Once);

            _contract.RescheduleCount.Should().Be(1);  // Started at 2, decremented to 1
            _contractRepoMock.Verify(r => r.UpdateAsync(_contract), Times.Once);

            request.Status.Should().Be("approved");
            request.StaffId.Should().Be(_staffId);
            _rescheduleRepoMock.Verify(r => r.UpdateAsync(request), Times.Once);
        }

        // Test: Ném lỗi khi duyệt request không tìm thấy
        [Fact]
        public async Task ApproveRequestAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((RescheduleRequest)null);
            Func<Task> act = () => _rescheduleService.ApproveRequestAsync(_staffId, Guid.NewGuid(), new ApproveRescheduleRequestDto());
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Reschedule request not found.");
        }

        // Test: Ném lỗi khi duyệt request không ở trạng thái "pending"
        [Fact]
        public async Task ApproveRequestAsync_RequestNotPending_ThrowsInvalidOperationException()
        {
            var request = new RescheduleRequest { Status = "approved" }; 
            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(request);
            Func<Task> act = () => _rescheduleService.ApproveRequestAsync(_staffId, Guid.NewGuid(), new ApproveRescheduleRequestDto());
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only pending requests can be approved.");
        }

        // Test: Ném lỗi khi duyệt request nhưng tutor không rảnh
        [Fact]
        public async Task ApproveRequestAsync_TutorNotAvailable_ThrowsInvalidOperationException()
        {
            var requestId = Guid.NewGuid();
            var newTutorId = Guid.NewGuid();
            var dto = new ApproveRescheduleRequestDto { NewTutorId = newTutorId };
            
            var newTutor = new User
            {
                UserId = newTutorId,
                RoleId = 2, // Tutor role
                Email = "tutor@example.com",
                FullName = "Test Tutor"
            };
            
            var request = new RescheduleRequest
            {
                RequestId = requestId,
                Status = "pending",
                Booking = _session,
                ContractId = _contractId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                StartTime = new TimeOnly(19, 0),
                EndTime = new TimeOnly(20, 30)
            };

            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _userRepoMock.Setup(u => u.GetByIdAsync(newTutorId)).ReturnsAsync(newTutor);
            _sessionRepoMock.Setup(r => r.IsTutorAvailableAsync(newTutorId, request.RequestedDate, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(false); 

            Func<Task> act = () => _rescheduleService.ApproveRequestAsync(_staffId, requestId, dto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Selected tutor is not available at the requested time.");
        }


        // Test: Từ chối request thành công
        [Fact]
        public async Task RejectRequestAsync_ValidRequest_RejectsRequest()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var reason = "Tutor bận";
            var request = new RescheduleRequest { RequestId = requestId, Status = "pending", Booking = _session };

            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _rescheduleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<RescheduleRequest>())).Returns(Task.CompletedTask);

            // Act
            var result = await _rescheduleService.RejectRequestAsync(_staffId, requestId, reason);

            // Assert
            result.Status.Should().Be("rejected");
            result.Message.Should().Be($"Request rejected: {reason}");

            request.Status.Should().Be("rejected");
            request.StaffId.Should().Be(_staffId);
            request.Reason.Should().Be(reason);
            _rescheduleRepoMock.Verify(r => r.UpdateAsync(request), Times.Once);
        }

        // Test: Ném lỗi khi từ chối request không tìm thấy
        [Fact]
        public async Task RejectRequestAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((RescheduleRequest)null);
            Func<Task> act = () => _rescheduleService.RejectRequestAsync(_staffId, Guid.NewGuid(), "reason");
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Reschedule request not found.");
        }

        // Test: Ném lỗi khi từ chối request không ở trạng thái "pending"
        [Fact]
        public async Task RejectRequestAsync_RequestNotPending_ThrowsInvalidOperationException()
        {
            var request = new RescheduleRequest { Status = "rejected" }; 
            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(request);
            Func<Task> act = () => _rescheduleService.RejectRequestAsync(_staffId, Guid.NewGuid(), "reason");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only pending requests can be rejected.");
        }

        // Test: Ném lỗi khi tutor mới không tồn tại
        [Fact]
        public async Task ApproveRequestAsync_NewTutorNotFound_ThrowsKeyNotFoundException()
        {
            var requestId = Guid.NewGuid();
            var newTutorId = Guid.NewGuid();
            var dto = new ApproveRescheduleRequestDto { NewTutorId = newTutorId };
            
            var request = new RescheduleRequest
            {
                RequestId = requestId,
                Status = "pending",
                Booking = _session,
                ContractId = _contractId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                StartTime = new TimeOnly(19, 0),
                EndTime = new TimeOnly(20, 30)
            };

            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _userRepoMock.Setup(u => u.GetByIdAsync(newTutorId)).ReturnsAsync((User)null);

            Func<Task> act = () => _rescheduleService.ApproveRequestAsync(_staffId, requestId, dto);
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Tutor not found.");
        }

        // Test: Ném lỗi khi user không phải là tutor
        [Fact]
        public async Task ApproveRequestAsync_NewUserIsNotTutor_ThrowsInvalidOperationException()
        {
            var requestId = Guid.NewGuid();
            var newUserId = Guid.NewGuid();
            var dto = new ApproveRescheduleRequestDto { NewTutorId = newUserId };
            
            var newUser = new User
            {
                UserId = newUserId,
                RoleId = 3, // Not a tutor (e.g., parent role)
                Email = "parent@example.com",
                FullName = "Test Parent"
            };
            
            var request = new RescheduleRequest
            {
                RequestId = requestId,
                Status = "pending",
                Booking = _session,
                ContractId = _contractId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                StartTime = new TimeOnly(19, 0),
                EndTime = new TimeOnly(20, 30)
            };

            _rescheduleRepoMock.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _userRepoMock.Setup(u => u.GetByIdAsync(newUserId)).ReturnsAsync(newUser);

            Func<Task> act = () => _rescheduleService.ApproveRequestAsync(_staffId, requestId, dto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Selected user is not a tutor.");
        }
    }
}
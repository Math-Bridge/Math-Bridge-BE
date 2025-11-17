using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class RescheduleServiceComprehensiveTests
    {
        private readonly Mock<IRescheduleRequestRepository> _resRepo;
        private readonly Mock<IContractRepository> _contractRepo;
        private readonly Mock<ISessionRepository> _sessionRepo;
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<IWalletTransactionRepository> _walletRepo;
        private readonly RescheduleService _service;

        public RescheduleServiceComprehensiveTests()
        {
            _resRepo = new Mock<IRescheduleRequestRepository>();
            _contractRepo = new Mock<IContractRepository>();
            _sessionRepo = new Mock<ISessionRepository>();
            _userRepo = new Mock<IUserRepository>();
            _walletRepo = new Mock<IWalletTransactionRepository>();
            _service = new RescheduleService(_resRepo.Object, _contractRepo.Object, _sessionRepo.Object, _userRepo.Object, _walletRepo.Object);
        }

        private Session BuildSession(Guid contractId, Guid tutorId, DateOnly sessionDate)
        {
            return new Session
            {
                BookingId = Guid.NewGuid(),
                ContractId = contractId,
                TutorId = tutorId,
                SessionDate = sessionDate,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Status = "scheduled",
                Contract = new Contract { ContractId = contractId, ParentId = _parentId }
            };
        }

        private readonly Guid _parentId = Guid.NewGuid();

        [Fact]
        public async Task CreateRequestAsync_InvalidStartTime_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                StartTime = new TimeOnly(15, 0), // invalid
                EndTime = new TimeOnly(16, 30),
                Reason = "Reason"
            };

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateRequestAsync_EndTimeNot90Minutes_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 0), // not 90 min
                Reason = "Reason"
            };

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateRequestAsync_SessionNotFound_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30)
            };
            _sessionRepo.Setup(r => r.GetByIdAsync(dto.BookingId)).ReturnsAsync((Session)null!);

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task CreateRequestAsync_NotParent_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30)
            };
            var session = BuildSession(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
            session.Contract.ParentId = Guid.NewGuid(); // different
            _sessionRepo.Setup(r => r.GetByIdAsync(dto.BookingId)).ReturnsAsync(session);

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task CreateRequestAsync_PastSession_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30)
            };
            var session = BuildSession(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
            _sessionRepo.Setup(r => r.GetByIdAsync(dto.BookingId)).ReturnsAsync(session);

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateRequestAsync_PendingExists_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30)
            };
            var session = BuildSession(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
            _sessionRepo.Setup(r => r.GetByIdAsync(dto.BookingId)).ReturnsAsync(session);
            _resRepo.Setup(r => r.HasPendingRequestForBookingAsync(dto.BookingId)).ReturnsAsync(true);

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateRequestAsync_ExceedsEndDate_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30)
            };
            var contractId = Guid.NewGuid();
            var session = BuildSession(contractId, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));
            _sessionRepo.Setup(r => r.GetByIdAsync(dto.BookingId)).ReturnsAsync(session);
            _resRepo.Setup(r => r.HasPendingRequestForBookingAsync(dto.BookingId)).ReturnsAsync(false);
            _contractRepo.Setup(r => r.GetByIdWithPackageAsync(contractId)).ReturnsAsync(new Contract
            {
                ContractId = contractId,
                Status = "active",
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                Package = new PaymentPackage { MaxReschedule = 3 },
                RescheduleCount = 0
            });

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateRequestAsync_MaxReached_Throws()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30)
            };
            var contractId = Guid.NewGuid();
            var session = BuildSession(contractId, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
            _sessionRepo.Setup(r => r.GetByIdAsync(dto.BookingId)).ReturnsAsync(session);
            _resRepo.Setup(r => r.HasPendingRequestForBookingAsync(dto.BookingId)).ReturnsAsync(false);
            _contractRepo.Setup(r => r.GetByIdWithPackageAsync(contractId)).ReturnsAsync(new Contract
            {
                ContractId = contractId,
                Status = "active",
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Package = new PaymentPackage { MaxReschedule = 3 },
                RescheduleCount = 0  // Zero attempts left should throw
            });

            await FluentActions.Invoking(() => _service.CreateRequestAsync(_parentId, dto))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateRequestAsync_Valid_ReturnsPending()
        {
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = Guid.NewGuid(),
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30),
                Reason = "Reason"
            };
            var contractId = Guid.NewGuid();
            var session = BuildSession(contractId, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
            _sessionRepo.Setup(r => r.GetByIdAsync(dto.BookingId)).ReturnsAsync(session);
            _resRepo.Setup(r => r.HasPendingRequestForBookingAsync(dto.BookingId)).ReturnsAsync(false);
            _contractRepo.Setup(r => r.GetByIdWithPackageAsync(contractId)).ReturnsAsync(new Contract
            {
                ContractId = contractId,
                Status = "active",
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                Package = new PaymentPackage { MaxReschedule = 3 },
                RescheduleCount = 2  // Some attempts left should succeed
            });
            _resRepo.Setup(r => r.AddAsync(It.IsAny<RescheduleRequest>())).Returns(Task.CompletedTask);

            var resp = await _service.CreateRequestAsync(_parentId, dto);
            resp.Status.Should().Be("pending");
        }

        [Fact]
        public async Task ApproveRequestAsync_TutorNotAvailable_Throws()
        {
            var staffId = Guid.NewGuid();
            var requestId = Guid.NewGuid();
            var bookingTutorId = Guid.NewGuid();
            var request = new RescheduleRequest
            {
                RequestId = requestId,
                Status = "pending",
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30),
                Booking = new Session { TutorId = bookingTutorId, IsOnline = true, SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }
            };
            _resRepo.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _sessionRepo.Setup(r => r.IsTutorAvailableAsync(bookingTutorId, request.RequestedDate, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(false);

            await FluentActions.Invoking(() => _service.ApproveRequestAsync(staffId, requestId, new ApproveRescheduleRequestDto()))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ApproveRequestAsync_Valid_UpdatesAndCreatesSession()
        {
            var staffId = Guid.NewGuid();
            var requestId = Guid.NewGuid();
            var bookingTutorId = Guid.NewGuid();
            var contractId = Guid.NewGuid();
            var contract = new Contract { ContractId = contractId, RescheduleCount = 0 };
            var request = new RescheduleRequest
            {
                RequestId = requestId,
                Status = "pending",
                ContractId = contractId,
                RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(17, 30),
                Booking = new Session { TutorId = bookingTutorId, IsOnline = true, Contract = contract, SessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }
            };
            _resRepo.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _sessionRepo.Setup(r => r.IsTutorAvailableAsync(bookingTutorId, request.RequestedDate, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(true);
            _sessionRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Session>>())).Returns(Task.CompletedTask);
            _sessionRepo.Setup(r => r.UpdateAsync(It.IsAny<Session>())).Returns(Task.CompletedTask);
            _contractRepo.Setup(r => r.UpdateAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            _resRepo.Setup(r => r.UpdateAsync(It.IsAny<RescheduleRequest>())).Returns(Task.CompletedTask);

            var resp = await _service.ApproveRequestAsync(staffId, requestId, new ApproveRescheduleRequestDto());
            resp.Status.Should().Be("approved");
            request.Booking.Status.Should().Be("rescheduled");
            contract.RescheduleCount.Should().Be(-1);
        }

        [Fact]
        public async Task RejectRequestAsync_Valid_Updates()
        {
            var staffId = Guid.NewGuid();
            var requestId = Guid.NewGuid();
            var request = new RescheduleRequest { RequestId = requestId, Status = "pending" };
            _resRepo.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(request);
            _resRepo.Setup(r => r.UpdateAsync(request)).Returns(Task.CompletedTask);

            var resp = await _service.RejectRequestAsync(staffId, requestId, "no");
            resp.Status.Should().Be("rejected");
        }

        [Fact]
        public async Task GetAvailableSubTutorsAsync_NoContract_Throws()
        {
            var requestId = Guid.NewGuid();
            _resRepo.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(new RescheduleRequest { RequestId = requestId, Contract = null! });
            await FluentActions.Invoking(() => _service.GetAvailableSubTutorsAsync(requestId))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetByIdAsync_ParentMismatch_Throws()
        {
            var requestId = Guid.NewGuid();
            var req = new RescheduleRequest { RequestId = requestId, ParentId = Guid.NewGuid(), Booking = new Session() };
            _resRepo.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(req);

            await FluentActions.Invoking(() => _service.GetByIdAsync(requestId, _parentId, "parent"))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task GetAllAsync_FilterByParent_ReturnsOnlyParent()
        {
            var parentId = Guid.NewGuid();
            _resRepo.Setup(r => r.GetByParentIdAsync(parentId)).ReturnsAsync(new List<RescheduleRequest>{ new RescheduleRequest{ ParentId = parentId, Booking = new Session() } });

            var list = await _service.GetAllAsync(parentId);
            list.Should().HaveCount(1);
        }
    }
}

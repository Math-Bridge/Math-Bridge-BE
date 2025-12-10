using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;

namespace MathBridgeSystem.Test.Service.Advanced
{
    public class RescheduleServiceEdgeTests
    {
        private readonly Mock<IRescheduleRequestRepository> _reqRepo = new();
        private readonly Mock<IContractRepository> _contractRepo = new();
        private readonly Mock<ISessionRepository> _sessionRepo = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IWalletTransactionRepository> _walletRepo = new();

        private RescheduleService CreateService() => new RescheduleService(_reqRepo.Object, _contractRepo.Object, _sessionRepo.Object, _userRepo.Object, _walletRepo.Object);

        private (Session session, Contract contract) BuildSessionContract(Guid parentId, Guid tutorId)
        {
            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                ParentId = parentId,
                Status = "active",
                Package = new PaymentPackage { MaxReschedule = 2 },
                RescheduleCount = 2,  // Start with some attempts available
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
            };
            var session = new Session
            {
                BookingId = Guid.NewGuid(),
                ContractId = contract.ContractId,
                Contract = contract,
                TutorId = tutorId,
                SessionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                StartTime = DateTime.Today.AddDays(2).AddHours(16),
                EndTime = DateTime.Today.AddDays(2).AddHours(17).AddMinutes(30),
                Status = "scheduled"
            };
            return (session, contract);
        }

        [Fact]
        public async Task CreateRequestAsync_InvalidStartTime_Throws()
        {
            var parentId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var (session, _) = BuildSessionContract(parentId, tutorId);
            _sessionRepo.Setup(s => s.GetByIdAsync(session.BookingId)).ReturnsAsync(session);
            _reqRepo.Setup(r => r.HasPendingRequestForBookingAsync(session.BookingId)).ReturnsAsync(false);
            _contractRepo.Setup(c => c.GetByIdWithPackageAsync(session.ContractId)).ReturnsAsync(session.Contract);
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = session.BookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                StartTime = new TimeOnly(15,0), // invalid
                EndTime = new TimeOnly(16,30),
                Reason = "Need change"
            };
            var service = CreateService();
            await FluentActions.Invoking(()=> service.CreateRequestAsync(parentId, dto)).Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateRequestAsync_EndTimeNot90Minutes_Throws()
        {
            var parentId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var (session, _) = BuildSessionContract(parentId, tutorId);
            _sessionRepo.Setup(s => s.GetByIdAsync(session.BookingId)).ReturnsAsync(session);
            _reqRepo.Setup(r => r.HasPendingRequestForBookingAsync(session.BookingId)).ReturnsAsync(false);
            _contractRepo.Setup(c => c.GetByIdWithPackageAsync(session.ContractId)).ReturnsAsync(session.Contract);
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = session.BookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                StartTime = new TimeOnly(16,0),
                EndTime = new TimeOnly(17,0), // should be 17:30
                Reason = "Need change"
            };
            var service = CreateService();
            await FluentActions.Invoking(()=> service.CreateRequestAsync(parentId, dto)).Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateRequestAsync_PendingExists_Throws()
        {
            var parentId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var (session, _) = BuildSessionContract(parentId, tutorId);
            _sessionRepo.Setup(s => s.GetByIdAsync(session.BookingId)).ReturnsAsync(session);
            _reqRepo.Setup(r => r.HasPendingRequestInContractAsync(session.ContractId)).ReturnsAsync(true);
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = session.BookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                StartTime = new TimeOnly(16,0),
                EndTime = new TimeOnly(17,30),
                Reason = "Need change"
            };
            var service = CreateService();
            await FluentActions.Invoking(()=> service.CreateRequestAsync(parentId, dto)).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateRequestAsync_ContractInactive_Throws()
        {
            var parentId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var (session, contract) = BuildSessionContract(parentId, tutorId);
            contract.Status = "pending"; // not active
            _sessionRepo.Setup(s => s.GetByIdAsync(session.BookingId)).ReturnsAsync(session);
            _reqRepo.Setup(r => r.HasPendingRequestInContractAsync(session.ContractId)).ReturnsAsync(false);
            _contractRepo.Setup(c => c.GetByIdWithPackageAsync(session.ContractId)).ReturnsAsync(contract);
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = session.BookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                StartTime = new TimeOnly(16,0),
                EndTime = new TimeOnly(17,30),
                Reason = "Need change"
            };
            var service = CreateService();
            await FluentActions.Invoking(()=> service.CreateRequestAsync(parentId, dto)).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateRequestAsync_ExceedsRescheduleCount_Throws()
        {
            var parentId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var (session, contract) = BuildSessionContract(parentId, tutorId);
            contract.RescheduleCount = 0; // No attempts left
            _sessionRepo.Setup(s => s.GetByIdAsync(session.BookingId)).ReturnsAsync(session);
            _reqRepo.Setup(r => r.HasPendingRequestInContractAsync(session.ContractId)).ReturnsAsync(false);
            _contractRepo.Setup(c => c.GetByIdWithPackageAsync(session.ContractId)).ReturnsAsync(contract);
            var dto = new CreateRescheduleRequestDto
            {
                BookingId = session.BookingId,
                RequestedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                StartTime = new TimeOnly(16,0),
                EndTime = new TimeOnly(17,30),
                Reason = "Need change"
            };
            var service = CreateService();
            await FluentActions.Invoking(()=> service.CreateRequestAsync(parentId, dto)).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ApproveRequestAsync_TutorNotAvailable_Throws()
        {
            var staffId = Guid.NewGuid();
            var requestId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var (session, contract) = BuildSessionContract(parentId, tutorId);
            var resReq = new RescheduleRequest
            {
                RequestId = requestId,
                Booking = session,
                BookingId = session.BookingId,
                Contract = contract,
                ContractId = contract.ContractId,
                RequestedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                StartTime = new TimeOnly(16,0),
                EndTime = new TimeOnly(17,30),
                Status = "pending"
            };
            _reqRepo.Setup(r => r.GetByIdWithDetailsAsync(requestId)).ReturnsAsync(resReq);
            _userRepo.Setup(u => u.GetByIdAsync(tutorId)).ReturnsAsync(new User { UserId = tutorId, RoleId = 2 });
            _sessionRepo.Setup(s => s.IsTutorAvailableAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(false);
            var service = CreateService();
            await FluentActions.Invoking(()=> service.ApproveRequestAsync(staffId, requestId, new ApproveRescheduleRequestDto())).Should().ThrowAsync<InvalidOperationException>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ContractServicePositiveTests
    {
        private readonly Mock<IContractRepository> _contractRepo = new();
        private readonly Mock<IPackageRepository> _packageRepo = new();
        private readonly Mock<ISessionRepository> _sessionRepo = new();
        private readonly Mock<IEmailService> _emailService = new();
        private ContractService CreateService() => new ContractService(_contractRepo.Object, _packageRepo.Object, _sessionRepo.Object, _emailService.Object);

        [Fact]
        public async Task CreateContract_GeneratesSessions_Success()
        {
            var request = new CreateContractRequest
            {
                ParentId = Guid.NewGuid(),
                ChildId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                CenterId = Guid.NewGuid(),
                MainTutorId = Guid.NewGuid(),
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
                StartTime = new TimeOnly(16,0),
                EndTime = new TimeOnly(17,30),
                DaysOfWeeks = 2 | 4, // Monday & Tuesday bits
                IsOnline = true,
                Status = "pending"
            };
            _packageRepo.Setup(p => p.GetByIdAsync(request.PackageId)).ReturnsAsync(new PaymentPackage{ PackageId = request.PackageId, SessionCount = 3, MaxReschedule = 1});
            _contractRepo.Setup(r => r.HasOverlappingContractForChildAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<byte?>(), null)).ReturnsAsync(false);
            List<Session> captured = new();
            _sessionRepo.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<Session>>()))
                .Callback<IEnumerable<Session>>(x => captured = x.ToList())
                .Returns(Task.CompletedTask);
            _contractRepo.Setup(r => r.AddAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);

            var service = CreateService();
            var id = await service.CreateContractAsync(request);

            id.Should().NotBeEmpty();
            captured.Should().HaveCount(3);
            captured.Should().OnlyContain(s => s.Status == "scheduled");
        }

        [Fact]
        public async Task UpdateStatus_Cancelled_CancelsScheduledAndRescheduled()
        {
            var contractId = Guid.NewGuid();
            _contractRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(new Contract{ ContractId=contractId, Status="active"});
            _contractRepo.Setup(r => r.UpdateAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            var sessions = new List<Session>
            {
                new Session{ BookingId = Guid.NewGuid(), Status = "scheduled" },
                new Session{ BookingId = Guid.NewGuid(), Status = "done" },
                new Session{ BookingId = Guid.NewGuid(), Status = "rescheduled" }
            };
            _sessionRepo.Setup(s => s.GetByContractIdAsync(contractId)).ReturnsAsync(sessions);
            _sessionRepo.Setup(s => s.UpdateAsync(It.IsAny<Session>())).Returns(Task.CompletedTask);

            var service = CreateService();
            var ok = await service.UpdateContractStatusAsync(contractId, new UpdateContractStatusRequest{ Status="cancelled"}, Guid.NewGuid());

            ok.Should().BeTrue();
            _sessionRepo.Verify(s => s.UpdateAsync(It.Is<Session>(x => x.Status == "cancelled")), Times.Exactly(2));
        }

        [Fact]
        public async Task GetContractsByParent_FormatsDaysOfWeek()
        {
            var parentId = Guid.NewGuid();
            var contracts = new List<Contract>
            {
                new Contract{ ContractId=Guid.NewGuid(), ParentId=parentId, ChildId=Guid.NewGuid(), PackageId=Guid.NewGuid(), StartDate=DateOnly.FromDateTime(DateTime.Today), EndDate=DateOnly.FromDateTime(DateTime.Today.AddDays(5)), DaysOfWeeks = 2 | 8 } // Monday & Wednesday => expect T2, T4
            };
            _contractRepo.Setup(r => r.GetByParentIdAsync(parentId)).ReturnsAsync(contracts);

            var service = CreateService();
            var result = await service.GetContractsByParentAsync(parentId);

            result.Should().HaveCount(1);
            result[0].DaysOfWeeksDisplay.Should().Contain("T2");
            result[0].DaysOfWeeksDisplay.Should().Contain("T4");
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using FluentAssertions;
//using Moq;
//using Xunit;
//using MathBridgeSystem.Application.Interfaces;
//using MathBridgeSystem.Application.DTOs;
//using MathBridgeSystem.Application.Services;
//using MathBridgeSystem.Domain.Entities;
//using MathBridgeSystem.Domain.Interfaces;

//namespace MathBridgeSystem.Test.Service.Advanced
//{
//    public class ContractServiceEdgeTests
//    {
//        private readonly Mock<IContractRepository> _contractRepo = new();
//        private readonly Mock<IPackageRepository> _packageRepo = new();
//        private readonly Mock<ISessionRepository> _sessionRepo = new();
//        private readonly Mock<IEmailService> _emailService = new();
//        private ContractService CreateService() => new ContractService(_contractRepo.Object, _packageRepo.Object, _sessionRepo.Object, _emailService.Object);

//        private CreateContractRequest BaseRequest(byte? daysMask = 2) => new CreateContractRequest
//        {
//            ParentId = Guid.NewGuid(),
//            ChildId = Guid.NewGuid(),
//            PackageId = Guid.NewGuid(),
//            CenterId = Guid.NewGuid(),
//            MainTutorId = Guid.NewGuid(),
//            StartDate = DateOnly.FromDateTime(DateTime.Today),
//            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
//            StartTime = new TimeOnly(9,0),
//            EndTime = new TimeOnly(10,30),
//            DaysOfWeeks = daysMask,
//            IsOnline = true,
//            Status = "pending"
//        };

//        [Fact]
//        public async Task CreateContractAsync_InvalidStatus_Throws()
//        {
//            var service = CreateService();
//            var req = BaseRequest();
//            req.Status = "bad";
//            _packageRepo.Setup(p => p.GetByIdAsync(req.PackageId)).ReturnsAsync(new PaymentPackage{ PackageId=req.PackageId, SessionCount=1, MaxReschedule=1});
//            await FluentActions.Invoking(()=> service.CreateContractAsync(req)).Should().ThrowAsync<ArgumentException>();
//        }

//        [Fact]
//        public async Task CreateContractAsync_DaysOfWeeks_OutOfRange_Throws()
//        {
//            var service = CreateService();
//            var req = BaseRequest(200); // >127
//            _packageRepo.Setup(p => p.GetByIdAsync(req.PackageId)).ReturnsAsync(new PaymentPackage{ PackageId=req.PackageId, SessionCount=1, MaxReschedule=1});
//            await FluentActions.Invoking(()=> service.CreateContractAsync(req)).Should().ThrowAsync<ArgumentOutOfRangeException>();
//        }

//        [Fact]
//        public async Task CreateContractAsync_DaysOfWeeks_Zero_Throws()
//        {
//            var service = CreateService();
//            var req = BaseRequest(0); // zero not allowed
//            _packageRepo.Setup(p => p.GetByIdAsync(req.PackageId)).ReturnsAsync(new PaymentPackage{ PackageId=req.PackageId, SessionCount=1, MaxReschedule=1});
//            await FluentActions.Invoking(()=> service.CreateContractAsync(req)).Should().ThrowAsync<ArgumentException>();
//        }

//        [Fact]
//        public async Task CreateContractAsync_OverlappingContract_Throws()
//        {
//            var service = CreateService();
//            var req = BaseRequest();
//            _packageRepo.Setup(p => p.GetByIdAsync(req.PackageId)).ReturnsAsync(new PaymentPackage{ PackageId=req.PackageId, SessionCount=1, MaxReschedule=1});
//            _contractRepo.Setup(r => r.HasOverlappingContractForChildAsync(req.ChildId, req.StartDate, req.EndDate, req.StartTime, req.EndTime, (byte?)req.DaysOfWeeks, null))
//                .ReturnsAsync(true);
//            await FluentActions.Invoking(()=> service.CreateContractAsync(req)).Should().ThrowAsync<InvalidOperationException>();
//        }

//        [Fact]
//        public async Task CreateContractAsync_NotEnoughDaysForSessions_Throws()
//        {
//            var service = CreateService();
//            var req = BaseRequest(2); // Only one day selected in 7 day window
//            // Package requires more sessions than available days
//            _packageRepo.Setup(p => p.GetByIdAsync(req.PackageId)).ReturnsAsync(new PaymentPackage{ PackageId=req.PackageId, SessionCount=3, MaxReschedule=1});
//            _contractRepo.Setup(r => r.HasOverlappingContractForChildAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<TimeOnly?>(), It.IsAny<byte?>(), null))
//                .ReturnsAsync(false);
//            await FluentActions.Invoking(()=> service.CreateContractAsync(req)).Should().ThrowAsync<InvalidOperationException>();
//        }

//        [Fact]
//        public async Task UpdateContractStatusAsync_InvalidStatus_Throws()
//        {
//            var service = CreateService();
//            var contractId = Guid.NewGuid();
//            _contractRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(new Contract{ ContractId=contractId, Status="pending"});
//            var req = new UpdateContractStatusRequest{ Status = "weird" };
//            await FluentActions.Invoking(()=> service.UpdateContractStatusAsync(contractId, req, Guid.NewGuid())).Should().ThrowAsync<ArgumentException>();
//        }

//        [Fact]
//        public async Task UpdateContractStatusAsync_ReactivateCancelled_Throws()
//        {
//            var service = CreateService();
//            var contractId = Guid.NewGuid();
//            _contractRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(new Contract{ ContractId=contractId, Status="cancelled"});
//            var req = new UpdateContractStatusRequest{ Status = "active" };
//            await FluentActions.Invoking(()=> service.UpdateContractStatusAsync(contractId, req, Guid.NewGuid())).Should().ThrowAsync<InvalidOperationException>();
//        }

//        [Fact]
//        public async Task AssignTutorsAsync_CancelledContract_Throws()
//        {
//            var service = CreateService();
//            var contractId = Guid.NewGuid();
//            _contractRepo.Setup(r => r.GetByIdWithPackageAsync(contractId)).ReturnsAsync(new Contract{ ContractId=contractId, Status="cancelled", Package = new PaymentPackage{ SessionCount=1}});
//            var req = new AssignTutorToContractRequest{ MainTutorId = Guid.NewGuid() };
//            await FluentActions.Invoking(()=> service.AssignTutorsAsync(contractId, req, Guid.NewGuid())).Should().ThrowAsync<InvalidOperationException>();
//        }

//        [Fact]
//        public async Task AssignTutorsAsync_MissingMainTutor_Throws()
//        {
//            var service = CreateService();
//            var contractId = Guid.NewGuid();
//            _contractRepo.Setup(r => r.GetByIdWithPackageAsync(contractId)).ReturnsAsync(new Contract{ ContractId=contractId, Status="pending", Package = new PaymentPackage{ SessionCount=1}});
//            var req = new AssignTutorToContractRequest{ MainTutorId = Guid.Empty };
//            await FluentActions.Invoking(()=> service.AssignTutorsAsync(contractId, req, Guid.NewGuid())).Should().ThrowAsync<ArgumentException>();
//        }
//    }
//}

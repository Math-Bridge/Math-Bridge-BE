using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.SePay;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MathBridgeSystem.Test.Service.Advanced
{
    public class SePayServiceEdgeTests
    {
        private readonly Mock<ISePayRepository> _sepayRepo = new();
        private readonly Mock<IWalletTransactionRepository> _walletRepo = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IContractRepository> _contractRepo = new();
        private readonly Mock<IPackageRepository> _packageRepo = new();
        private readonly Mock<ILogger<SePayService>> _logger = new();
        private readonly Mock<IEmailService> _emailService = new();
        private readonly Mock<INotificationService> _notificationService = new();

        private SePayService CreateService(IConfiguration? cfg = null) => new SePayService(
            _sepayRepo.Object,
            _walletRepo.Object,
            _userRepo.Object,
            _contractRepo.Object,
            _packageRepo.Object,
            cfg ?? new ConfigurationBuilder().AddInMemoryCollection().Build(),
            _logger.Object,
            _emailService.Object,
            _notificationService.Object
        );

        [Fact]
        public async Task CreatePaymentRequestAsync_InvalidAmount_ReturnsFailure()
        {
            var service = CreateService();
            var resp = await service.CreatePaymentRequestAsync(new SePayPaymentRequestDto{ UserId=Guid.NewGuid(), Amount = 0, Description = "x"});
            resp.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreatePaymentRequestAsync_UserNotFound_ReturnsFailure()
        {
            var service = CreateService();
            _userRepo.Setup(u => u.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);
            var resp = await service.CreatePaymentRequestAsync(new SePayPaymentRequestDto{ UserId=Guid.NewGuid(), Amount = 100, Description = "x"});
            resp.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreatePaymentRequestAsync_Success_PopulatesFields()
        {
            var service = CreateService();
            var uid = Guid.NewGuid();
            _userRepo.Setup(u => u.GetByIdAsync(uid)).ReturnsAsync(new User{ UserId=uid });
            _walletRepo.Setup(w => w.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync((WalletTransaction wt)=> wt);
            var resp = await service.CreatePaymentRequestAsync(new SePayPaymentRequestDto{ UserId=uid, Amount = 150000, Description = "TopUp"});
            resp.Success.Should().BeTrue();
            resp.QrCodeUrl.Should().NotBeNullOrWhiteSpace();
            resp.OrderReference.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void ExtractOrderReference_ReturnsNullIfNotFound()
        {
            var service = CreateService();
            service.ExtractOrderReference("NoRefHere").Should().BeNull();
        }

        [Fact]
        public void ExtractOrderReference_FindsPrefix()
        {
            var service = CreateService(new ConfigurationBuilder().AddInMemoryCollection(new[]{new KeyValuePair<string,string>("SePay:OrderReferencePrefix","MB")}).Build());
            service.ExtractOrderReference("PAY MBABC123 done").Should().StartWith("MB");
        }

        [Fact]
        public void GenerateQrCodeUrl_EncodesDescription()
        {
            var service = CreateService(new ConfigurationBuilder().AddInMemoryCollection(new[]{new KeyValuePair<string,string>("SePay:QrBaseUrl","https://qr.example.com/img"), new KeyValuePair<string,string>("SePay:BankCode","MB"), new KeyValuePair<string,string>("SePay:AccountNumber","123"), new KeyValuePair<string,string>("SePay:AccountName","John")}).Build());
            var url = service.GenerateQrCodeUrl(12345, "MB123 TEST SPACE");
            url.Should().Contain("MB123+TEST+SPACE");
        }
    }
}

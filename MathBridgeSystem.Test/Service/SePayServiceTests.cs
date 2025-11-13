using FluentAssertions;
using MathBridgeSystem.Application.DTOs.SePay;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Web;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MathBridgeSystem.Tests.Services
{
    public class SePayServiceTests
    {
        private readonly Mock<ISePayRepository> _sePayRepositoryMock;
        private readonly Mock<IWalletTransactionRepository> _walletTransactionRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly Mock<IPackageRepository> _packageRepositoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<SePayService>> _loggerMock;
        private readonly SePayService _sePayService;

        // Dữ liệu config giả lập
        private const string _bankCode = "MBBANK";
        private const string _accountNumber = "123456789";
        private const string _accountName = "TEST USER";
        private const string _qrBaseUrl = "https://qr.sepay.vn/img";
        private const string _orderReferencePrefix = "MB";

        public SePayServiceTests()
        {
            _sePayRepositoryMock = new Mock<ISePayRepository>();
            _walletTransactionRepositoryMock = new Mock<IWalletTransactionRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _contractRepositoryMock = new Mock<IContractRepository>();
            _packageRepositoryMock = new Mock<IPackageRepository>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<SePayService>>();

            // Giả lập IConfiguration trả về các giá trị
            _configurationMock.SetupGet(c => c["SePay:BankCode"]).Returns(_bankCode);
            _configurationMock.SetupGet(c => c["SePay:AccountNumber"]).Returns(_accountNumber);
            _configurationMock.SetupGet(c => c["SePay:AccountName"]).Returns(_accountName);
            _configurationMock.SetupGet(c => c["SePay:QrBaseUrl"]).Returns(_qrBaseUrl);
            _configurationMock.SetupGet(c => c["SePay:OrderReferencePrefix"]).Returns(_orderReferencePrefix);

            _sePayService = new SePayService(
                _sePayRepositoryMock.Object,
                _walletTransactionRepositoryMock.Object,
                _userRepositoryMock.Object,
                _contractRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _configurationMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods Tests (GenerateQrCodeUrl, ExtractOrderReference, etc.)

        // Test: Hàm GenerateQrCodeUrl (đồng thời kiểm tra config đã load đúng)
        [Fact]
        public void GenerateQrCodeUrl_ValidInput_ReturnsCorrectlyFormattedUrl()
        {
            // Arrange
            decimal amount = 150000;
            string description = "MB1234";
            var encodedDescription = HttpUtility.UrlEncode(description);
            string expectedUrl = $"{_qrBaseUrl}?bank={_bankCode}&acc={_accountNumber}&template=compact&amount={amount:F0}&des={encodedDescription}";

            // Act
            var result = _sePayService.GenerateQrCodeUrl(amount, description);

            // Assert
            result.Should().Be(expectedUrl);
        }

        // Test: Hàm ExtractOrderReference (với prefix 'MB' từ config)
        [Theory]
        [InlineData("Thanh toan MB1234ABC", "MB1234ABC")]
        [InlineData("mb5678def", "mb5678def")] 
        [InlineData("MB1234", "MB1234")]
        [InlineData("No prefix 1234", null)]
        [InlineData("MB ABC", null)] // Regex yêu cầu A-Z0-9, không có khoảng trắng
        public void ExtractOrderReference_VariousContent_ReturnsCorrectReferenceOrNull(string content, string expected)
        {
            // Act
            var result = _sePayService.ExtractOrderReference(content);

            // Assert
            result.Should().Be(expected);
        }

        // Test: Hàm ValidateWebhookSignature (luôn trả về true)
        [Fact]
        public void ValidateWebhookSignature_Always_ReturnsTrue()
        {
            // Act
            var result = _sePayService.ValidateWebhookSignature("payload", "signature");

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region CreatePaymentRequestAsync Tests (Nạp tiền ví)

        // Test: Ném lỗi khi số tiền <= 0
        [Fact]
        public async Task CreatePaymentRequestAsync_AmountIsZero_ReturnsFailure()
        {
            // Arrange
            var request = new SePayPaymentRequestDto { UserId = Guid.NewGuid(), Amount = 0 };

            // Act
            var result = await _sePayService.CreatePaymentRequestAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Amount must be greater than 0");
        }

        // Test: Ném lỗi khi không tìm thấy User
        [Fact]
        public async Task CreatePaymentRequestAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new SePayPaymentRequestDto { UserId = Guid.NewGuid(), Amount = 1000 };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync((User)null);

            // Act
            var result = await _sePayService.CreatePaymentRequestAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("User not found");
        }

        // Test: Ném lỗi nếu Repository thất bại
        [Fact]
        public async Task CreatePaymentRequestAsync_RepositoryFails_ReturnsFailure()
        {
            // Arrange
            var request = new SePayPaymentRequestDto { UserId = Guid.NewGuid(), Amount = 1000 };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _walletTransactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ThrowsAsync(new Exception("DB Error"));

            // Act
            var result = await _sePayService.CreatePaymentRequestAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("An error occurred while creating payment request");
        }

        // Test: Tạo request nạp ví thành công
        [Fact]
        public async Task CreatePaymentRequestAsync_ValidRequest_CreatesTransactionsAndReturnsSuccess()
        {
            // Arrange
            var request = new SePayPaymentRequestDto { UserId = Guid.NewGuid(), Amount = 50000, Description = "Nap tien" };
            var transactionId = Guid.NewGuid();
            var walletTx = new WalletTransaction { TransactionId = transactionId };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(request.UserId)).ReturnsAsync(new User());
            _walletTransactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(walletTx);

            // Act
            var result = await _sePayService.CreatePaymentRequestAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Amount.Should().Be(50000);
            result.WalletTransactionId.Should().Be(transactionId);
            result.BankInfo.Should().Be($"{_accountName} - {_accountNumber} - {_bankCode}");

            var expectedRef = $"{_orderReferencePrefix}{transactionId.ToString("N")[..8].ToUpper()}";
            result.OrderReference.Should().Be(expectedRef);
            result.TransferContent.Should().Be(expectedRef);

            _walletTransactionRepositoryMock.Verify(r => r.AddAsync(It.Is<WalletTransaction>(w => w.Amount == 50000 && w.Status == "Pending")), Times.Once);
            _sePayRepositoryMock.Verify(r => r.AddAsync(It.Is<SepayTransaction>(s => s.OrderReference == expectedRef && s.WalletTransactionId == transactionId)), Times.Once);
            _walletTransactionRepositoryMock.Verify(r => r.UpdateAsync(It.Is<WalletTransaction>(w => w.PaymentGatewayReference == expectedRef)), Times.Once);
        }

        #endregion

        #region CreateContractDirectPaymentAsync Tests (Thanh toán Hợp đồng)

        // Test: Ném lỗi khi Contract không tìm thấy
        [Fact]
        public async Task CreateContractDirectPaymentAsync_ContractNotFound_ReturnsFailure()
        {
            _contractRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Contract)null);
            var result = await _sePayService.CreateContractDirectPaymentAsync(Guid.NewGuid(), Guid.NewGuid());
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Contract not found");
        }

        // Test: Ném lỗi khi User không sở hữu Hợp đồng
        [Fact]
        public async Task CreateContractDirectPaymentAsync_UserNotAuthorized_ReturnsFailure()
        {
            var contract = new Contract { ParentId = Guid.NewGuid() };
            _contractRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(contract);

            var result = await _sePayService.CreateContractDirectPaymentAsync(Guid.NewGuid(), Guid.NewGuid()); 

            result.Success.Should().BeFalse();
            result.Message.Should().Be("User is not authorized for this contract");
        }

        // Test: Ném lỗi khi Package không tìm thấy
        [Fact]
        public async Task CreateContractDirectPaymentAsync_PackageNotFound_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var contract = new Contract { ParentId = userId, PackageId = Guid.NewGuid() };
            _contractRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(contract);
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(contract.PackageId)).ReturnsAsync((PaymentPackage)null);

            var result = await _sePayService.CreateContractDirectPaymentAsync(Guid.NewGuid(), userId);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Payment package not found");
        }

        // Test: Tạo thanh toán Hợp đồng thành công
        [Fact]
        public async Task CreateContractDirectPaymentAsync_ValidRequest_CreatesSepayTransactionAndReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var contractId = Guid.NewGuid();
            var contract = new Contract { ContractId = contractId, ParentId = userId, PackageId = Guid.NewGuid(), Status = "Pending" };
            var package = new PaymentPackage { Price = 150000 };

            _contractRepositoryMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(contract.PackageId)).ReturnsAsync(package);

            // Act
            var result = await _sePayService.CreateContractDirectPaymentAsync(contractId, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Amount.Should().Be(150000);
            result.WalletTransactionId.Should().Be(Guid.Empty); 

            _contractRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Contract>(c => c.Status == "unpaid")), Times.Once);
            _sePayRepositoryMock.Verify(r => r.AddAsync(It.Is<SepayTransaction>(s => s.ContractId == contractId && s.TransferAmount == 150000)), Times.Once);
            _walletTransactionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WalletTransaction>()), Times.Never);
        }

        #endregion

        #region ProcessWebhookAsync Tests (Xử lý Webhook)

        // Test: Webhook cho Hợp đồng - Xử lý thành công
        [Fact]
        public async Task ProcessWebhookAsync_ContractPayment_Valid_UpdatesContractStatus()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var webhook = new SePayWebhookRequestDto { Code = "MB_CONTRACT", TransferAmount = 150000 };
            var sepayTx = new SepayTransaction { ContractId = contractId, TransferAmount = 150000 };
            var contract = new Contract { ContractId = contractId, Status = "unpaid" };

            _sePayRepositoryMock.Setup(r => r.GetByCodeAsync(webhook.Code)).ReturnsAsync(sepayTx);
            _contractRepositoryMock.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

            // Act
            var result = await _sePayService.ProcessWebhookAsync(webhook);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Contract payment webhook processed successfully");
            _contractRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Contract>(c => c.Status == "Pending")), Times.Once);
            _sePayRepositoryMock.Verify(r => r.UpdateAsync(sepayTx), Times.Once);
            _walletTransactionRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WalletTransaction>()), Times.Never);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        // Test: Webhook cho Nạp ví - Xử lý thành công
        [Fact]
        public async Task ProcessWebhookAsync_WalletTopUp_Valid_UpdatesWalletAndTransaction()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var transactionId = Guid.NewGuid();
            var orderRef = "MB12345678";
            var webhook = new SePayWebhookRequestDto { Code = "tx_code", Content = $"Thanh toan {orderRef}", TransferAmount = 100000 };

            var walletTx = new WalletTransaction { TransactionId = transactionId, ParentId = userId, Amount = 100000, Status = "Pending" };
            var sepayTx = new SepayTransaction { OrderReference = orderRef, WalletTransaction = walletTx, WalletTransactionId = transactionId };
            var user = new User { UserId = userId, WalletBalance = 0 };

            _sePayRepositoryMock.Setup(r => r.GetByCodeAsync(webhook.Code)).ReturnsAsync((SepayTransaction)null);
            _sePayRepositoryMock.Setup(r => r.ExistsByCodeAsync(webhook.Code)).ReturnsAsync(sepayTx);
            _sePayRepositoryMock.Setup(r => r.GetByOrderReferenceAsync(orderRef)).ReturnsAsync(sepayTx);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _sePayService.ProcessWebhookAsync(webhook);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Webhook processed successfully");
            result.WalletTransactionId.Should().Be(transactionId);

            _walletTransactionRepositoryMock.Verify(r => r.UpdateAsync(It.Is<WalletTransaction>(w => w.Status == "Completed")), Times.Once);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.WalletBalance == 100000)), Times.Once);
            _sePayRepositoryMock.Verify(r => r.UpdateAsync(sepayTx), Times.Once);
        }

        // Test: Webhook - Ném lỗi khi không tìm thấy giao dịch
        [Fact]
        public async Task ProcessWebhookAsync_TransactionNotFound_ReturnsFailure()
        {
            var webhook = new SePayWebhookRequestDto { Code = "NOT_FOUND" };
            _sePayRepositoryMock.Setup(r => r.GetByCodeAsync(webhook.Code)).ReturnsAsync((SepayTransaction)null);
            _sePayRepositoryMock.Setup(r => r.ExistsByCodeAsync(webhook.Code)).ReturnsAsync((SepayTransaction)null);

            var result = await _sePayService.ProcessWebhookAsync(webhook);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("No transaction found");
        }

        // Test: Webhook - Báo thành công nếu giao dịch đã được xử lý
        [Fact]
        public async Task ProcessWebhookAsync_TransactionAlreadyProcessed_ReturnsSuccess()
        {
            var webhook = new SePayWebhookRequestDto { Code = "PROCESSED" };
            var walletTx = new WalletTransaction { Status = "Completed" };
            var sepayTx = new SepayTransaction { WalletTransaction = walletTx };
            _sePayRepositoryMock.Setup(r => r.GetByCodeAsync(webhook.Code)).ReturnsAsync((SepayTransaction)null);
            _sePayRepositoryMock.Setup(r => r.ExistsByCodeAsync(webhook.Code)).ReturnsAsync(sepayTx);

            var result = await _sePayService.ProcessWebhookAsync(webhook);

            result.Success.Should().BeTrue();
            result.Message.Should().Be("Transaction already processed");
        }

        // Test: Webhook - Ném lỗi khi không trích xuất được OrderReference
        [Fact]
        public async Task ProcessWebhookAsync_OrderReferenceExtractFails_ReturnsFailure()
        {
            var webhook = new SePayWebhookRequestDto { Code = "tx_code", Content = "No reference" }; 
            var walletTx = new WalletTransaction { Status = "Pending" };
            var sepayTx = new SepayTransaction { WalletTransaction = walletTx };
            _sePayRepositoryMock.Setup(r => r.GetByCodeAsync(webhook.Code)).ReturnsAsync((SepayTransaction)null);
            _sePayRepositoryMock.Setup(r => r.ExistsByCodeAsync(webhook.Code)).ReturnsAsync(sepayTx);

            var result = await _sePayService.ProcessWebhookAsync(webhook);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Order reference not found in transaction content");
        }

        // Test: Webhook - Ném lỗi khi sai số tiền
        [Fact]
        public async Task ProcessWebhookAsync_AmountMismatch_ReturnsFailure()
        {
            var orderRef = "MB12345678";
            var webhook = new SePayWebhookRequestDto { Code = "tx_code", Content = orderRef, TransferAmount = 100000 };
            var walletTx = new WalletTransaction { Status = "Pending", Amount = 50000 }; 
            var sepayTx = new SepayTransaction { OrderReference = orderRef, WalletTransaction = walletTx };

            _sePayRepositoryMock.Setup(r => r.GetByCodeAsync(webhook.Code)).ReturnsAsync((SepayTransaction)null);
            _sePayRepositoryMock.Setup(r => r.ExistsByCodeAsync(webhook.Code)).ReturnsAsync(sepayTx);
            _sePayRepositoryMock.Setup(r => r.GetByOrderReferenceAsync(orderRef)).ReturnsAsync(sepayTx);

            var result = await _sePayService.ProcessWebhookAsync(webhook);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Transaction amount does not match");
        }

        #endregion

        #region CheckPaymentStatusAsync Tests

        // Test: Kiểm tra trạng thái (Không tìm thấy)
        [Fact]
        public async Task CheckPaymentStatusAsync_TransactionNotFound_ReturnsNotFoundStatus()
        {
            _walletTransactionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((WalletTransaction)null);
            var result = await _sePayService.CheckPaymentStatusAsync(Guid.NewGuid());

            result.Success.Should().BeFalse();
            result.Status.Should().Be("NotFound");
        }

        // Test: Kiểm tra trạng thái (Đang chờ)
        [Fact]
        public async Task CheckPaymentStatusAsync_TransactionPending_ReturnsUnpaidStatus()
        {
            var txId = Guid.NewGuid();
            var walletTx = new WalletTransaction { TransactionId = txId, Status = "Pending" };
            _walletTransactionRepositoryMock.Setup(r => r.GetByIdAsync(txId)).ReturnsAsync(walletTx);
            _sePayRepositoryMock.Setup(r => r.GetByWalletTransactionIdAsync(txId)).ReturnsAsync(new SepayTransaction());

            var result = await _sePayService.CheckPaymentStatusAsync(txId);

            result.Success.Should().BeTrue();
            result.Status.Should().Be("Unpaid");
        }

        // Test: Kiểm tra trạng thái (Hoàn thành)
        [Fact]
        public async Task CheckPaymentStatusAsync_TransactionCompleted_ReturnsPaidStatus()
        {
            var txId = Guid.NewGuid();
            var paidDate = DateTime.UtcNow.ToLocalTime().AddMinutes(-5);
            var walletTx = new WalletTransaction { TransactionId = txId, Status = "Completed", Amount = 1000 };
            var sepayTx = new SepayTransaction { TransactionDate = paidDate };

            _walletTransactionRepositoryMock.Setup(r => r.GetByIdAsync(txId)).ReturnsAsync(walletTx);
            _sePayRepositoryMock.Setup(r => r.GetByWalletTransactionIdAsync(txId)).ReturnsAsync(sepayTx);

            var result = await _sePayService.CheckPaymentStatusAsync(txId);

            result.Success.Should().BeTrue();
            result.Status.Should().Be("Paid");
            result.AmountPaid.Should().Be(1000);
            result.PaidAt.Should().Be(paidDate);
        }

        #endregion

        #region GetPaymentDetailsAsync Tests

        // Test: Lấy chi tiết thanh toán (Không tìm thấy WalletTx)
        [Fact]
        public async Task GetPaymentDetailsAsync_WalletTransactionNotFound_ReturnsNull()
        {
            _walletTransactionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((WalletTransaction)null);
            var result = await _sePayService.GetPaymentDetailsAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // Test: Lấy chi tiết thanh toán (Không tìm thấy SepayTx)
        [Fact]
        public async Task GetPaymentDetailsAsync_SepayTransactionNotFound_ReturnsNull()
        {
            _walletTransactionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new WalletTransaction());
            _sePayRepositoryMock.Setup(r => r.GetByWalletTransactionIdAsync(It.IsAny<Guid>())).ReturnsAsync((SepayTransaction)null);

            var result = await _sePayService.GetPaymentDetailsAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        // Test: Lấy chi tiết thanh toán (Thành công)
        [Fact]
        public async Task GetPaymentDetailsAsync_Found_ReturnsDto()
        {
            var txId = Guid.NewGuid();
            var walletTx = new WalletTransaction { TransactionId = txId, Amount = 12345 };
            var sepayTx = new SepayTransaction { OrderReference = "MB_TEST" };

            _walletTransactionRepositoryMock.Setup(r => r.GetByIdAsync(txId)).ReturnsAsync(walletTx);
            _sePayRepositoryMock.Setup(r => r.GetByWalletTransactionIdAsync(txId)).ReturnsAsync(sepayTx);

            var result = await _sePayService.GetPaymentDetailsAsync(txId);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Amount.Should().Be(12345);
            result.OrderReference.Should().Be("MB_TEST");
            result.BankInfo.Should().Be($"{_accountName} - {_accountNumber} - {_bankCode}");
        }

        #endregion
    }
}
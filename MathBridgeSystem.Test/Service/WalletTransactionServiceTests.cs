using FluentAssertions;
using MathBridgeSystem.Application.DTOs.WalletTransaction;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Test.Service
{
    public class WalletTransactionServiceTests
    {
        private readonly Mock<IWalletTransactionRepository> _transactionRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly WalletTransactionService _service;

        public WalletTransactionServiceTests()
        {
            _transactionRepositoryMock = new Mock<IWalletTransactionRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _contractRepositoryMock = new Mock<IContractRepository>();

            _service = new WalletTransactionService(
                _transactionRepositoryMock.Object,
                _userRepositoryMock.Object,
                _contractRepositoryMock.Object
            );
        }

        [Fact]
        public async Task GetTransactionByIdAsync_ShouldReturnTransaction_WhenExists()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var transaction = new WalletTransaction
            {
                TransactionId = transactionId,
                ParentId = parentId,
                Amount = 100.00m,
                TransactionType = "Deposit",
                Status = "Completed"
            };

            _transactionRepositoryMock.Setup(r => r.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Act
            var result = await _service.GetTransactionByIdAsync(transactionId);

            // Assert
            result.Should().NotBeNull();
            result.TransactionId.Should().Be(transactionId);
            result.Amount.Should().Be(100.00m);
        }

        [Fact]
        public async Task GetTransactionByIdAsync_ShouldThrowKeyNotFoundException_WhenNotExists()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            _transactionRepositoryMock.Setup(r => r.GetByIdAsync(transactionId))
                .ReturnsAsync((WalletTransaction)null!);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.GetTransactionByIdAsync(transactionId)
            );
        }

        [Fact]
        public async Task GetTransactionsByParentIdAsync_ShouldReturnAllParentTransactions()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var transactions = new List<WalletTransaction>
            {
                new WalletTransaction { TransactionId = Guid.NewGuid(), ParentId = parentId, Amount = 100m },
                new WalletTransaction { TransactionId = Guid.NewGuid(), ParentId = parentId, Amount = 50m }
            };

            _transactionRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId))
                .ReturnsAsync(transactions);

            // Act
            var result = await _service.GetTransactionsByParentIdAsync(parentId);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateTransactionAsync_ShouldCreateTransaction_WhenParentExists()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var parent = new User { UserId = parentId };
            var request = new CreateWalletTransactionRequest
            {
                ParentId = parentId,
                Amount = 100m,
                TransactionType = "Deposit",
                Description = "Test deposit",
                PaymentMethod = "Credit Card"
            };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(parentId))
                .ReturnsAsync(parent);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>()))
                .ReturnsAsync((WalletTransaction t) => t);

            // Act
            var result = await _service.CreateTransactionAsync(request);

            // Assert
            result.Should().NotBeEmpty();
            _transactionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WalletTransaction>()), Times.Once);
        }

        [Fact]
        public async Task CreateTransactionAsync_ShouldThrowArgumentException_WhenParentNotExists()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var request = new CreateWalletTransactionRequest
            {
                ParentId = parentId,
                Amount = 100m,
                TransactionType = "Deposit"
            };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(parentId))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateTransactionAsync(request)
            );
        }

        [Fact]
        public async Task UpdateTransactionStatusAsync_ShouldUpdateStatus_WhenTransactionExists()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transaction = new WalletTransaction
            {
                TransactionId = transactionId,
                Status = "Pending"
            };

            _transactionRepositoryMock.Setup(r => r.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            // Act
            await _service.UpdateTransactionStatusAsync(transactionId, "Completed");

            // Assert
            transaction.Status.Should().Be("Completed");
            _transactionRepositoryMock.Verify(r => r.UpdateAsync(transaction), Times.Once);
        }

        [Fact]
        public async Task GetParentWalletBalanceAsync_ShouldCalculateCorrectBalance()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var transactions = new List<WalletTransaction>
            {
                new WalletTransaction { TransactionType = "Deposit", Amount = 100m, Status = "Completed" },
                new WalletTransaction { TransactionType = "Payment", Amount = 30m, Status = "Completed" },
                new WalletTransaction { TransactionType = "Refund", Amount = 10m, Status = "Completed" },
                new WalletTransaction { TransactionType = "Deposit", Amount = 50m, Status = "Pending" } // Should be ignored
            };

            _transactionRepositoryMock.Setup(r => r.GetByParentIdAsync(parentId))
                .ReturnsAsync(transactions);

            // Act
            var result = await _service.GetParentWalletBalanceAsync(parentId);

            // Assert
            result.Should().Be(80m); // 100 - 30 + 10 = 80
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenTransactionRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new WalletTransactionService(
                null!,
                _userRepositoryMock.Object,
                _contractRepositoryMock.Object
            );

            action.Should().Throw<ArgumentNullException>();
        }
    }
}


using MathBridgeSystem.Application.DTOs.WalletTransaction;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class WalletTransactionService : IWalletTransactionService
    {
        private readonly IWalletTransactionRepository _transactionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IContractRepository _contractRepository;

        public WalletTransactionService(
            IWalletTransactionRepository transactionRepository,
            IUserRepository userRepository,
            IContractRepository contractRepository)
        {
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
        }

        public async Task<WalletTransactionDto> GetTransactionByIdAsync(Guid transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");

            return MapToDto(transaction);
        }

        public async Task<IEnumerable<WalletTransactionDto>> GetTransactionsByParentIdAsync(Guid parentId)
        {
            var transactions = await _transactionRepository.GetByParentIdAsync(parentId);
            return transactions.Select(MapToDto);
        }

        public async Task<Guid> CreateTransactionAsync(CreateWalletTransactionRequest request)
        {
            // Validate parent exists
            var parent = await _userRepository.GetByIdAsync(request.ParentId);
            if (parent == null)
                throw new ArgumentException($"Parent with ID {request.ParentId} not found.");

            // Validate contract if provided
            if (request.ContractId.HasValue)
            {
                var contract = await _contractRepository.GetByIdAsync(request.ContractId.Value);
                if (contract == null)
                    throw new ArgumentException($"Contract with ID {request.ContractId} not found.");
            }

            var transaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = request.ParentId,
                ContractId = request.ContractId,
                Amount = request.Amount,
                TransactionType = request.TransactionType,
                Description = request.Description,
                TransactionDate = DateTime.UtcNow.ToLocalTime(),
                Status = "Pending",
                PaymentMethod = request.PaymentMethod,
                PaymentGatewayReference = request.PaymentGatewayReference,
                PaymentGateway = request.PaymentGateway
            };

            var createdTransaction = await _transactionRepository.AddAsync(transaction);
            return createdTransaction.TransactionId;
        }

        public async Task UpdateTransactionStatusAsync(Guid transactionId, string status)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");

            transaction.Status = status;
            await _transactionRepository.UpdateAsync(transaction);
        }

        public async Task<decimal> GetParentWalletBalanceAsync(Guid parentId)
        {
            var transactions = await _transactionRepository.GetByParentIdAsync(parentId);
            var completedTransactions = transactions.Where(t => t.Status == "Completed");

            decimal balance = 0;
            foreach (var transaction in completedTransactions)
            {
                if (transaction.TransactionType == "Deposit" || transaction.TransactionType == "Refund")
                    balance += transaction.Amount;
                else if (transaction.TransactionType == "Withdrawal" || transaction.TransactionType == "Payment")
                    balance -= transaction.Amount;
            }

            return balance;
        }

        private WalletTransactionDto MapToDto(WalletTransaction transaction)
        {
            return new WalletTransactionDto
            {
                TransactionId = transaction.TransactionId,
                ParentId = transaction.ParentId,
                ParentName = transaction.Parent?.FullName ?? "Unknown",
                ContractId = transaction.ContractId,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                Status = transaction.Status,
                PaymentMethod = transaction.PaymentMethod,
                PaymentGatewayReference = transaction.PaymentGatewayReference,
                PaymentGateway = transaction.PaymentGateway
            };
        }
    }
}
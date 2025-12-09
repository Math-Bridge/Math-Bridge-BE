using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MathBridgeSystem.Application.DTOs.Withdrawal;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Infrastructure.Data;

namespace MathBridgeSystem.Application.Services
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly MathBridgeDbContext _context;

        public WithdrawalService(MathBridgeDbContext context)
        {
            _context = context;
        }

        public async Task<WithdrawalRequest> RequestWithdrawalAsync(Guid userId, WithdrawalRequestCreateDto requestDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found");

            // Check available balance (considering pending withdrawals if necessary, but per request just wallet check)
            if (user.WalletBalance < requestDto.Amount)
            {
                throw new Exception("Insufficient wallet balance");
            }

            var request = new WithdrawalRequest
            {
                Id = Guid.NewGuid(),
                ParentId = userId,
                Amount = requestDto.Amount,
                BankName = requestDto.BankName,
                BankAccountNumber = requestDto.BankAccountNumber,
                BankHolderName = requestDto.BankHolderName,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            _context.WithdrawalRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<WithdrawalRequest> ProcessWithdrawalAsync(Guid withdrawalId, Guid staffId)
        {
            var request = await _context.WithdrawalRequests
                .Include(r => r.Parent)
                .FirstOrDefaultAsync(r => r.Id == withdrawalId);

            if (request == null) throw new Exception("Withdrawal request not found");
            if (request.Status != "Pending") throw new Exception("Request is not in Pending status");

            // Deduct money
            if (request.Parent.WalletBalance < request.Amount)
            {
                throw new Exception("Insufficient balance at processing time");
            }

            // Create Wallet Transaction
            var transaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = request.ParentId,
                Amount = -request.Amount,
                TransactionType = "Withdrawal",
                Description = $"Withdrawal to {request.BankName} - {request.BankAccountNumber}",
                TransactionDate = DateTime.UtcNow,
                Status = "Success",
                PaymentMethod = "Bank Transfer"
            };

            request.Parent.WalletBalance -= request.Amount;
            request.Status = "Processed";
            request.ProcessedDate = DateTime.UtcNow;
            request.StaffId = staffId;

            _context.WalletTransactions.Add(transaction);
            _context.Users.Update(request.Parent);
            _context.WithdrawalRequests.Update(request);

            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<IEnumerable<WithdrawalRequest>> GetPendingRequestsAsync()
        {
            return await _context.WithdrawalRequests
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.CreatedDate)
                .Include(r => r.Parent)
                .ToListAsync();
        }

        public async Task<IEnumerable<WithdrawalRequest>> GetMyRequestsAsync(Guid userId)
        {
             return await _context.WithdrawalRequests
                .Where(r => r.ParentId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }
    }
}
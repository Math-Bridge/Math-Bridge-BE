using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MathBridgeSystem.Application.DTOs.Withdrawal;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Domain.Interfaces;

namespace MathBridgeSystem.Application.Services
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly MathBridgeDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public WithdrawalService(
            MathBridgeDbContext context, 
            IUserRepository userRepository,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _context = context;
            _userRepository = userRepository;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        private static WithdrawalResponseDTO MapToDto(WithdrawalRequest request)
        {
            return new WithdrawalResponseDTO
            {
                Id = request.Id,
                ParentId = request.ParentId,
                Amount = request.Amount,
                BankName = request.BankName,
                BankAccountNumber = request.BankAccountNumber,
                BankHolderName = request.BankHolderName,
                Status = request.Status,
                CreatedDate = request.CreatedDate,
                ProcessedDate = request.ProcessedDate,
                StaffId = request.StaffId
            };
        }

        public async Task<WithdrawalResponseDTO> RequestWithdrawalAsync(Guid userId, WithdrawalRequestCreateDto requestDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
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
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            _context.WithdrawalRequests.Add(request);
            await _context.SaveChangesAsync();

            // Send email notification to parent
            try
            {
                await _emailService.SendWithdrawalRequestCreatedAsync(
                    user.Email,
                    user.FullName,
                    request.Amount,
                    request.BankName,
                    request.BankAccountNumber,
                    request.CreatedDate);
            }
            catch
            {
                // Log but don't fail the withdrawal request if email fails
            }

            // Create in-app notification
            try
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = userId,
                    Title = "Withdrawal Request Submitted",
                    Message = $"Your withdrawal request for {request.Amount:N0} VND has been submitted and is pending review.",
                    NotificationType = "Withdrawal"
                });
            }
            catch
            {
                // Log but don't fail the withdrawal request if notification fails
            }

            return MapToDto(request);
        }

        public async Task<WithdrawalResponseDTO> ProcessWithdrawalAsync(Guid withdrawalId, Guid staffId)
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
                Amount = request.Amount,
                TransactionType = "withdrawal",
                Description = $"Withdrawal to {request.BankName} - {request.BankAccountNumber}",
                TransactionDate = DateTime.UtcNow.ToLocalTime(),
                Status = "completed",
                PaymentMethod = "Bank Transfer"
            };

            request.Parent.WalletBalance -= request.Amount;
            request.Status = "Processed";
            request.ProcessedDate = DateTime.UtcNow.ToLocalTime();
            request.StaffId = staffId;

            _context.WalletTransactions.Add(transaction);
            _context.Users.Update(request.Parent);
            _context.WithdrawalRequests.Update(request);

            await _context.SaveChangesAsync();

            // Send email notification to parent
            try
            {
                await _emailService.SendWithdrawalProcessedAsync(
                    request.Parent.Email,
                    request.Parent.FullName,
                    request.Amount,
                    request.BankName,
                    request.BankAccountNumber,
                    request.ProcessedDate ?? DateTime.UtcNow.ToLocalTime());
            }
            catch
            {
                // Log but don't fail the withdrawal process if email fails
            }

            // Create in-app notification
            try
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = request.ParentId,
                    Title = "Withdrawal Processed",
                    Message = $"Your withdrawal request for {request.Amount:N0} VND has been processed successfully. The funds will be transferred to your bank account.",
                    NotificationType = "Withdrawal"
                });
            }
            catch
            {
                // Log but don't fail the withdrawal process if notification fails
            }

            return MapToDto(request);
        }

        public async Task<IEnumerable<WithdrawalResponseDTO>> GetPendingRequestsAsync()
        {
            var requests = await _context.WithdrawalRequests
                .OrderByDescending(r => r.CreatedDate)
                .Include(r => r.Parent)
                .ToListAsync();
            return requests.Select(MapToDto);
        }

        public async Task<IEnumerable<WithdrawalResponseDTO>> GetMyRequestsAsync(Guid userId)
        {
            var requests = await _context.WithdrawalRequests
                .Where(r => r.ParentId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            return requests.Select(MapToDto);
        }
    }
}
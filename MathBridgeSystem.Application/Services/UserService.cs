using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;

        public UserService(IUserRepository userRepository, IWalletTransactionRepository walletTransactionRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _walletTransactionRepository = walletTransactionRepository ?? throw new ArgumentNullException(nameof(walletTransactionRepository));
        }

        public async Task<UserResponse> GetUserByIdAsync(Guid id, Guid currentUserId, string currentUserRole)
        {
            if (string.IsNullOrEmpty(currentUserRole) || (currentUserRole != "admin" && currentUserId != id))
                throw new Exception("Unauthorized access");

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("User not found");

            return new UserResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                WalletBalance = user.WalletBalance,
                RoleId = user.RoleId,
                Status = user.Status,
                FormattedAddress = user.FormattedAddress,
                placeId = user.GooglePlaceId
            };
        }

        public async Task<Guid> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid currentUserId, string currentUserRole)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(currentUserRole) || (currentUserRole != "admin" && currentUserId != id))
                throw new Exception("Unauthorized access");

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("User not found");

            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.Gender = request.Gender;

            await _userRepository.UpdateAsync(user);
            return user.UserId;
        }

        public async Task<WalletResponse> GetWalletAsync(Guid parentId, Guid currentUserId, string currentUserRole)
        {
            if (string.IsNullOrEmpty(currentUserRole) || (currentUserRole != "admin" && currentUserId != parentId))
                throw new Exception("Unauthorized access");

            var user = await _userRepository.GetByIdAsync(parentId);
            if (user == null || user.Role.RoleName != "parent")
                throw new Exception("Invalid parent user");

            var transactions = await _walletTransactionRepository.GetByParentIdAsync(parentId);

            return new WalletResponse
            {
                WalletBalance = user.WalletBalance,
                Transactions = transactions.Select(t => new WalletResponse.WalletTransactionDto
                {
                    TransactionId = t.TransactionId,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    TransactionDate = t.TransactionDate,
                    Status = t.Status
                }).ToList()
            };
        }

        public async Task<Guid> AdminCreateUserAsync(RegisterRequest request, string currentUserRole)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(currentUserRole) || currentUserRole != "admin")
                throw new Exception("Only admins can create users");

            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new Exception("Email already exists");

            var roleExists = await _userRepository.RoleExistsAsync(request.RoleId);
            if (!roleExists)
                throw new Exception($"Invalid RoleId: {request.RoleId}");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                RoleId = request.RoleId,
                WalletBalance = 0.00m,
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                LastActive = DateTime.UtcNow.ToLocalTime(),
                Status = "active"
            };

            await _userRepository.AddAsync(user);
            return user.UserId;
        }

        public async Task<Guid> UpdateUserStatusAsync(Guid id, UpdateStatusRequest request, string currentUserRole)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(currentUserRole) || currentUserRole != "admin")
                throw new Exception("Only admins can update user status");

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("User not found");

            user.Status = request.Status;
            await _userRepository.UpdateAsync(user);
            return user.UserId;
        }

        public async Task<DeductWalletResponse> DeductWalletAsync(Guid parentId, Guid id, Guid currentUserId, string currentUserRole)
        {
            // Authorization check: only the parent themselves or admin can deduct from wallet
            if (string.IsNullOrEmpty(currentUserRole) || (currentUserRole != "admin" && currentUserId != parentId))
                throw new Exception("Unauthorized access");

            // Get the parent user
            var parent = await _userRepository.GetByIdAsync(parentId);
            if (parent == null || parent.Role.RoleName != "parent")
                throw new Exception("Invalid parent user");

            // Get the contract with package details
            var contract = await _userRepository.GetContractWithPackageAsync(id);
            if (contract == null)
                throw new Exception("Contract not found");

            // Verify the contract belongs to the parent
            if (contract.ParentId != parentId)
                throw new Exception("Contract does not belong to this parent");

            // Get the package price
            decimal packagePrice = contract.Package.Price;

            // Check if parent has sufficient balance
            if (parent.WalletBalance < packagePrice)
                throw new Exception($"Insufficient wallet balance. Required: {packagePrice:N2}, Available: {parent.WalletBalance:N2}");

            // Create wallet transaction
            var transaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = parentId,
                ContractId = id,
                Amount = packagePrice,
                TransactionType = "withdrawal",
                Description = $"Payment for contract {id} - Package: {contract.Package.PackageName}",
                TransactionDate = DateTime.UtcNow.ToLocalTime(),
                Status = "completed",
                PaymentMethod = "wallet",
                PaymentGateway = "internal"
            };

            // Deduct from parent's wallet balance
            parent.WalletBalance -= packagePrice;

            // Save transaction and update user balance
            await _walletTransactionRepository.AddAsync(transaction);
            await _userRepository.UpdateAsync(parent);

            return new DeductWalletResponse
            {
                TransactionId = transaction.TransactionId,
                AmountDeducted = packagePrice,
                NewWalletBalance = parent.WalletBalance,
                TransactionStatus = transaction.Status,
                TransactionDate = transaction.TransactionDate,
                Message = "Wallet deduction successful"
            };
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync(string currentUserRole)
        {
            // Only admins can get all users
            if (string.IsNullOrEmpty(currentUserRole) || currentUserRole != "admin")
                throw new Exception("Only admins can view all users");

            var users = await _userRepository.GetAllAsync();

            return users.Select(user => new UserResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                WalletBalance = user.WalletBalance,
                RoleId = user.RoleId,
                Status = user.Status,
                FormattedAddress = user.FormattedAddress,
                placeId = user.GooglePlaceId
            });
        }
    }
}

using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using MathBridge.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridge.Application.Services
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
                Status = user.Status
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
                CreatedDate = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
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
    }
}
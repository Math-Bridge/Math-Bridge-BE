﻿using MathBridgeSystem.Application.DTOs;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserResponse> GetUserByIdAsync(Guid id, Guid currentUserId, string currentUserRole);
        Task<Guid> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid currentUserId, string currentUserRole);
        Task<WalletResponse> GetWalletAsync(Guid parentId, Guid currentUserId, string currentUserRole);
        Task<Guid> AdminCreateUserAsync(RegisterRequest request, string currentUserRole);
        Task<Guid> UpdateUserStatusAsync(Guid id, UpdateStatusRequest request, string currentUserRole);

        Task<DeductWalletResponse> DeductWalletAsync(Guid parentId, DeductWalletRequest request, Guid currentUserId, string currentUserRole);
        
        Task<IEnumerable<UserResponse>> GetAllUsersAsync(string currentUserRole);
    }
}
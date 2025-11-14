using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid id);

        Task<Contract?> GetContractWithPackageAsync(Guid contractId);
        Task<User> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<List<User>> GetUsersWithLocationAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<bool> RoleExistsAsync(int roleId);
        Task<Role> GetRoleByIdAsync(int roleId);
        Task<List<User>> GetAllAsync();
        Task<bool> ExistsAsync(Guid id);
        Task<List<User>> GetTutorsAsync();
        Task<List<User>> GetTutorsByCenterAsync(Guid centerId);
        Task<User> GetTutorWithVerificationAsync(Guid tutorId);
    }
}
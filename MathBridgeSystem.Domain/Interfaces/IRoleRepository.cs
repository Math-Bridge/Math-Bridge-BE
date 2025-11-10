using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(int id);
        Task<Role?> GetByIdAsync(int id);
        Task<List<Role>> GetAllAsync();
        Task<Role?> GetByNameAsync(string roleName);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNameAsync(string roleName);
    }
}
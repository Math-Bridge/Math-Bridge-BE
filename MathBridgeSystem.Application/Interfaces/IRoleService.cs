using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IRoleService
    {
        Task<int> CreateRoleAsync(CreateRoleRequest request);
        Task UpdateRoleAsync(int id, UpdateRoleRequest request);
        Task DeleteRoleAsync(int id);
        Task<RoleDto?> GetRoleByIdAsync(int id);
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<RoleDto?> GetRoleByNameAsync(string roleName);
    }
}
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
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        }

        public async Task<int> CreateRoleAsync(CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
                throw new ArgumentException("Role name is required.");

            // Check if role already exists
            if (await _roleRepository.ExistsByNameAsync(request.RoleName))
                throw new InvalidOperationException($"Role with name '{request.RoleName}' already exists.");

            var role = new Role
            {
                RoleName = request.RoleName.Trim(),
                Description = request.Description?.Trim()
            };

            await _roleRepository.AddAsync(role);
            return role.RoleId;
        }

        public async Task UpdateRoleAsync(int id, UpdateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
                throw new ArgumentException("Role name is required.");

            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                throw new InvalidOperationException("Role not found.");

            // Check if new role name already exists (excluding current role)
            var existingRole = await _roleRepository.GetByNameAsync(request.RoleName);
            if (existingRole != null && existingRole.RoleId != id)
                throw new InvalidOperationException($"Role with name '{request.RoleName}' already exists.");

            role.RoleName = request.RoleName.Trim();
            role.Description = request.Description?.Trim();

            await _roleRepository.UpdateAsync(role);
        }

        public async Task DeleteRoleAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                throw new InvalidOperationException("Role not found.");

            // Check if role has users
            if (role.Users != null && role.Users.Any())
                throw new InvalidOperationException("Cannot delete role with assigned users.");

            await _roleRepository.DeleteAsync(id);
        }

        public async Task<RoleDto?> GetRoleByIdAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                return null;

            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                UserCount = role.Users?.Count ?? 0
            };
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                UserCount = r.Users?.Count ?? 0
            }).ToList();
        }

        public async Task<RoleDto?> GetRoleByNameAsync(string roleName)
        {
            var role = await _roleRepository.GetByNameAsync(roleName);
            if (role == null)
                return null;

            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                UserCount = role.Users?.Count ?? 0
            };
        }
    }
}
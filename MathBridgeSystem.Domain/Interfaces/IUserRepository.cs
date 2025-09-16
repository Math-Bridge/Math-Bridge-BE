using MathBridge.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace MathBridge.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<List<User>> GetUsersWithLocationAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<bool> RoleExistsAsync(int roleId);
    }
}
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MathBridgeDbContext _context;

        public UserRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<User> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.TutorVerification)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.TutorVerification)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetUsersWithLocationAsync()
        {
            return await _context.Users
                .Where(u => u.Latitude.HasValue && u.Longitude.HasValue && u.Status == "active")
                .Include(u => u.Role)
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> RoleExistsAsync(int roleId)
        {
            return await _context.Roles.AnyAsync(r => r.RoleId == roleId);
        }

        // Added implementations for CenterService and other features
        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.TutorVerification)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Users.AnyAsync(u => u.UserId == id);
        }

        public async Task<List<User>> GetTutorsAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.TutorVerification)
                .Include(u => u.TutorCenters)
                    .ThenInclude(tc => tc.Center)
                .Where(u => u.RoleId == 2)
                .Where(u => u.Status == "active")
                .ToListAsync();
        }

        public async Task<List<User>> GetTutorsByCenterAsync(Guid centerId)
        {
            return await _context.TutorCenters
                .Include(tc => tc.Tutor)
                    .ThenInclude(t => t.Role)
                .Include(tc => tc.Tutor)
                    .ThenInclude(t => t.TutorVerification)
                .Where(tc => tc.CenterId == centerId)
                .Select(tc => tc.Tutor)
                .Where(t => t.Status == "active")
                .ToListAsync();
        }

        public async Task<User> GetTutorWithVerificationAsync(Guid tutorId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.TutorVerification)
                .Include(u => u.TutorCenters)
                    .ThenInclude(tc => tc.Center)
                .FirstOrDefaultAsync(u => u.UserId == tutorId && u.RoleId == 2);
        }
    }
}

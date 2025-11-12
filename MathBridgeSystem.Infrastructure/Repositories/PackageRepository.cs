using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class PackageRepository : IPackageRepository
    {
        private readonly MathBridgeDbContext _context;

        public PackageRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(PaymentPackage package)
        {
            _context.PaymentPackages.Add(package);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PaymentPackage>> GetAllAsync()
        {
            return await _context.PaymentPackages.ToListAsync();
        }
        
        public async Task<PaymentPackage> GetByIdAsync(Guid id)
        {
            return await _context.PaymentPackages.FirstOrDefaultAsync(p => p.PackageId == id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.PaymentPackages.AnyAsync(p => p.PackageId == id);
        }

        public async Task<bool> ExistsCurriculumAsync(Guid curriculumId)
        {
            return await _context.Curricula.AnyAsync(c => c.CurriculumId == curriculumId);
        }

        public async Task UpdateAsync(PaymentPackage package)
        {
            _context.PaymentPackages.Update(package);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var package = await GetByIdAsync(id);
            if (package != null)
            {
                _context.PaymentPackages.Remove(package);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsPackageInUseAsync(Guid packageId)
        {
            return await _context.Contracts.AnyAsync(c => c.PackageId == packageId);
        }
        public async Task<PaymentPackage> GetPackageByCurriculumIdAsync(Guid curriculumId)
        {
            return await _context.PaymentPackages
                .FirstOrDefaultAsync(p => p.CurriculumId == curriculumId);
        }

        public async Task<List<PaymentPackage>> GetAllActivePackagesAsync()
        {
            return await _context.PaymentPackages
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        public async Task<PaymentPackage> GetActivePackageByIdAsync(Guid id)
        {
            return await _context.PaymentPackages
                .FirstOrDefaultAsync(p => p.PackageId == id && p.IsActive);
        }
    }
}
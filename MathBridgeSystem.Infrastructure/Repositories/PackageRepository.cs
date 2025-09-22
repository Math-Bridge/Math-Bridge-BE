using MathBridge.Infrastructure.Data;
using MathBridge.Domain.Entities;
using MathBridge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Infrastructure.Repositories
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
            return await _context.PaymentPackages
                .Include(p => p.Program)
                .ToListAsync();
        }

        public async Task<PaymentPackage> GetByIdAsync(Guid id)
        {
            return await _context.PaymentPackages
                .Include(p => p.Program)
                .FirstOrDefaultAsync(p => p.PackageId == id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.PaymentPackages.AnyAsync(p => p.PackageId == id);
        }
    }
}
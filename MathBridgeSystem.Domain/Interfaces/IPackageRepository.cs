using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IPackageRepository
    {
        Task AddAsync(PaymentPackage package);
        // Task<List<PaymentPackage>> GetAllAsync();
        // Task<PaymentPackage> GetByIdAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}

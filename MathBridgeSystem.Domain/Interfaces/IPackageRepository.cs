using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IPackageRepository
    {
        Task AddAsync(PaymentPackage package);
        Task<List<PaymentPackage>> GetAllAsync();
        Task<PaymentPackage> GetByIdAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsCurriculumAsync(Guid curriculumId);
        Task UpdateAsync(PaymentPackage package);
        Task DeleteAsync(Guid id);
        Task<bool> IsPackageInUseAsync(Guid packageId);
    }
}
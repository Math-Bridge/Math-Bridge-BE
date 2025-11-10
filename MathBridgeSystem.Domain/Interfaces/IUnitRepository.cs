using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IUnitRepository
    {
        Task AddAsync(Unit unit);
        Task UpdateAsync(Unit unit);
        Task DeleteAsync(Guid id);
        Task<Unit?> GetByIdAsync(Guid id);
        Task<List<Unit>> GetAllAsync();
        Task<List<Unit>> GetByCurriculumIdAsync(Guid curriculumId);
        Task<Unit?> GetByNameAsync(string unitName);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByNameAsync(string unitName, Guid curriculumId);
        Task<int> GetMaxUnitOrderAsync(Guid curriculumId);
    }
}
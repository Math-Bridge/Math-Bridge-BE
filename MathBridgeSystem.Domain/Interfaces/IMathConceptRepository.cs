using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IMathConceptRepository
    {
        Task AddAsync(MathConcept mathConcept);
        Task UpdateAsync(MathConcept mathConcept);
        Task DeleteAsync(Guid id);
        Task<MathConcept?> GetByIdAsync(Guid id);
        Task<List<MathConcept>> GetAllAsync();
        Task<List<MathConcept>> GetByUnitIdAsync(Guid unitId);
        Task<MathConcept?> GetByNameAsync(string name);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByNameAsync(string name);
        Task<List<MathConcept>> GetByCategoryAsync(string category);
        Task<bool> IsMathConceptLinkedToUnitsAsync(Guid conceptId);
        Task<List<MathConcept>> GetByIdsAsync(List<Guid> ids);
    }
}

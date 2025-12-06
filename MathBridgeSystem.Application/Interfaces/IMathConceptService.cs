using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IMathConceptService
    {
        Task<Guid> CreateMathConceptAsync(CreateMathConceptRequest request);
        Task UpdateMathConceptAsync(Guid id, UpdateMathConceptRequest request);
        Task DeleteMathConceptAsync(Guid id);
        Task<MathConceptDto?> GetMathConceptByIdAsync(Guid id);
        Task<List<MathConceptDto>> GetAllMathConceptsAsync();
        Task<List<MathConceptDto>> GetMathConceptsByUnitIdAsync(Guid unitId);
        Task<MathConceptDto?> GetMathConceptByNameAsync(string name);
        Task<List<MathConceptDto>> GetMathConceptsByCategoryAsync(string category);
        Task LinkMathConceptToUnitsAsync(Guid conceptId, List<Guid> unitIds);
        Task UnlinkMathConceptFromUnitsAsync(Guid conceptId, List<Guid> unitIds);
    }
}

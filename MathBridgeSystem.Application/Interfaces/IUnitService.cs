using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IUnitService
    {
        Task<Guid> CreateUnitAsync(CreateUnitRequest request, Guid? createdBy = null);
        Task UpdateUnitAsync(Guid id, UpdateUnitRequest request, Guid? updatedBy = null);
        Task DeleteUnitAsync(Guid id);
        Task<UnitDto?> GetUnitByIdAsync(Guid id);
        Task<List<UnitDto>> GetAllUnitsAsync();
        Task<List<UnitDto>> GetUnitsByCurriculumIdAsync(Guid curriculumId);
        Task<UnitDto?> GetUnitByNameAsync(string unitName);
    }
}
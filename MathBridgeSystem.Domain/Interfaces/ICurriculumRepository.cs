using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ICurriculumRepository
    {
        // Basic CRUD Methods
        Task AddAsync(Curriculum curriculum);
        Task UpdateAsync(Curriculum curriculum);
        Task<Curriculum?> GetByIdAsync(Guid curriculumId);
        Task<List<Curriculum>> GetAllAsync();
        Task DeleteAsync(Guid curriculumId);

        // Query Methods
        Task<List<Curriculum>> GetActiveAsync();
        Task<Curriculum?> GetByCodeAsync(string code);
        Task<bool> ExistsByCodeAsync(string code);

        // Related Data Methods
        Task<int> GetSchoolsCountAsync(Guid curriculumId);
        Task<int> GetPackagesCountAsync(Guid curriculumId);
        Task<List<School>> GetSchoolsByCurriculumIdAsync(Guid curriculumId);
    }
}
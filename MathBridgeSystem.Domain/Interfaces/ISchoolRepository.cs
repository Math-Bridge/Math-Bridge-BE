using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ISchoolRepository
    {
        // Basic CRUD Methods
        Task AddAsync(School school);
        Task UpdateAsync(School school);
        Task<School?> GetByIdAsync(Guid schoolId);
        Task<List<School>> GetAllAsync();
        Task DeleteAsync(Guid schoolId);

        // Query Methods
        Task<List<School>> GetActiveSchoolsAsync();
        Task<List<School>> GetSchoolsByCurriculumIdAsync(Guid curriculumId);
        Task<School?> GetByNameAsync(string schoolName);
        Task<bool> ExistsAsync(Guid schoolId);
        Task<bool> ExistsByNameAsync(string schoolName);

        // Related Data Methods
        Task<Curriculum?> GetCurriculumByIdAsync(Guid curriculumId);
        Task<int> GetChildrenCountAsync(Guid schoolId);
        Task<List<Child>> GetChildrenBySchoolIdAsync(Guid schoolId);
    }
}
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.School;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ISchoolService
    {
        // CRUD Operations
        Task<Guid> CreateSchoolAsync(CreateSchoolRequest request);
        Task UpdateSchoolAsync(Guid schoolId, UpdateSchoolRequest request);
        Task DeleteSchoolAsync(Guid schoolId);
        Task ActivateSchoolAsync(Guid schoolId);
        Task DeactivateSchoolAsync(Guid schoolId);

        // Query Operations
        Task<SchoolDto?> GetSchoolByIdAsync(Guid schoolId);
        Task<List<SchoolDto>> GetAllSchoolsAsync();
        Task<List<SchoolDto>> GetActiveSchoolsAsync();
        Task<List<SchoolDto>> SearchSchoolsAsync(SchoolSearchRequest request);
        Task<int> GetSchoolsCountAsync(SchoolSearchRequest request);
        Task<List<SchoolDto>> GetSchoolsByCurriculumAsync(Guid curriculumId);

        // Related Data Operations
        Task<SchoolWithChildrenDto?> GetSchoolWithChildrenAsync(Guid schoolId);
        Task<List<ChildDto>> GetChildrenBySchoolAsync(Guid schoolId);
    }
}
using MathBridgeSystem.Application.DTOs.Curriculum;
using MathBridgeSystem.Application.DTOs.School;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ICurriculumService
    {
        // CRUD Operations
        Task<Guid> CreateCurriculumAsync(CreateCurriculumRequest request);
        Task UpdateCurriculumAsync(Guid curriculumId, UpdateCurriculumRequest request);
        Task DeleteCurriculumAsync(Guid curriculumId);
        Task ActivateCurriculumAsync(Guid curriculumId);
        Task DeactivateCurriculumAsync(Guid curriculumId);

        // Query Operations
        Task<CurriculumDto?> GetCurriculumByIdAsync(Guid curriculumId);
        Task<List<CurriculumDto>> GetAllCurriculaAsync();
        Task<List<CurriculumDto>> GetActiveCurriculaAsync();
        Task<List<CurriculumDto>> SearchCurriculaAsync(CurriculumSearchRequest request);
        Task<int> GetCurriculaCountAsync(CurriculumSearchRequest request);

        // Related Data Operations
        Task<CurriculumWithSchoolsDto?> GetCurriculumWithSchoolsAsync(Guid curriculumId);
        Task<List<SchoolDto>> GetSchoolsByCurriculumAsync(Guid curriculumId);
    }
}
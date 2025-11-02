using MathBridgeSystem.Application.DTOs.Curriculum;
using MathBridgeSystem.Application.DTOs.School;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class CurriculumService : ICurriculumService
    {
        private readonly ICurriculumRepository _curriculumRepository;

        public CurriculumService(ICurriculumRepository curriculumRepository)
        {
            _curriculumRepository = curriculumRepository ?? throw new ArgumentNullException(nameof(curriculumRepository));
        }

        // CRUD Operations
        public async Task<Guid> CreateCurriculumAsync(CreateCurriculumRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.CurriculumCode))
                throw new ArgumentException("Curriculum code is required", nameof(request.CurriculumCode));

            if (string.IsNullOrWhiteSpace(request.CurriculumName))
                throw new ArgumentException("Curriculum name is required", nameof(request.CurriculumName));

            if (string.IsNullOrWhiteSpace(request.Grades))
                throw new ArgumentException("Grades are required", nameof(request.Grades));

            // Check for duplicate code
            var existsByCode = await _curriculumRepository.ExistsByCodeAsync(request.CurriculumCode);
            if (existsByCode)
                throw new ArgumentException($"Curriculum with code '{request.CurriculumCode}' already exists");

            var curriculum = new Curriculum
            {
                CurriculumId = Guid.NewGuid(),
                CurriculumCode = request.CurriculumCode,
                CurriculumName = request.CurriculumName,
                Grades = request.Grades,
                SyllabusUrl = request.SyllabusUrl,
                TotalCredits = request.TotalCredits,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _curriculumRepository.AddAsync(curriculum);
            return curriculum.CurriculumId;
        }

        public async Task UpdateCurriculumAsync(Guid curriculumId, UpdateCurriculumRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var curriculum = await _curriculumRepository.GetByIdAsync(curriculumId);
            if (curriculum == null)
                throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found");

            bool hasChanges = false;

            // Update CurriculumCode if provided
            if (!string.IsNullOrWhiteSpace(request.CurriculumCode) && request.CurriculumCode != curriculum.CurriculumCode)
            {
                var existsByCode = await _curriculumRepository.ExistsByCodeAsync(request.CurriculumCode);
                if (existsByCode)
                    throw new ArgumentException($"Curriculum with code '{request.CurriculumCode}' already exists");

                curriculum.CurriculumCode = request.CurriculumCode;
                hasChanges = true;
            }

            // Update CurriculumName if provided
            if (!string.IsNullOrWhiteSpace(request.CurriculumName) && request.CurriculumName != curriculum.CurriculumName)
            {
                curriculum.CurriculumName = request.CurriculumName;
                hasChanges = true;
            }

            // Update Grades if provided
            if (!string.IsNullOrWhiteSpace(request.Grades) && request.Grades != curriculum.Grades)
            {
                curriculum.Grades = request.Grades;
                hasChanges = true;
            }

            // Update SyllabusUrl if provided
            if (request.SyllabusUrl != null && request.SyllabusUrl != curriculum.SyllabusUrl)
            {
                curriculum.SyllabusUrl = request.SyllabusUrl;
                hasChanges = true;
            }

            // Update TotalCredits if provided
            if (request.TotalCredits.HasValue && request.TotalCredits.Value != curriculum.TotalCredits)
            {
                curriculum.TotalCredits = request.TotalCredits.Value;
                hasChanges = true;
            }

            // Update IsActive if provided
            if (request.IsActive.HasValue && request.IsActive.Value != curriculum.IsActive)
            {
                curriculum.IsActive = request.IsActive.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                curriculum.UpdatedDate = DateTime.UtcNow;
                await _curriculumRepository.UpdateAsync(curriculum);
            }
        }

        public async Task DeleteCurriculumAsync(Guid curriculumId)
        {
            var curriculum = await _curriculumRepository.GetByIdAsync(curriculumId);
            if (curriculum == null)
                throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found");

            var schoolsCount = await _curriculumRepository.GetSchoolsCountAsync(curriculumId);
            if (schoolsCount > 0)
                throw new InvalidOperationException($"Cannot delete curriculum with ID {curriculumId} because it has {schoolsCount} associated schools");

            var packagesCount = await _curriculumRepository.GetPackagesCountAsync(curriculumId);
            if (packagesCount > 0)
                throw new InvalidOperationException($"Cannot delete curriculum with ID {curriculumId} because it has {packagesCount} associated payment packages");

            await _curriculumRepository.DeleteAsync(curriculumId);
        }

        public async Task ActivateCurriculumAsync(Guid curriculumId)
        {
            var curriculum = await _curriculumRepository.GetByIdAsync(curriculumId);
            if (curriculum == null)
                throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found");

            if (!curriculum.IsActive)
            {
                curriculum.IsActive = true;
                curriculum.UpdatedDate = DateTime.UtcNow;
                await _curriculumRepository.UpdateAsync(curriculum);
            }
        }

        public async Task DeactivateCurriculumAsync(Guid curriculumId)
        {
            var curriculum = await _curriculumRepository.GetByIdAsync(curriculumId);
            if (curriculum == null)
                throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found");

            if (curriculum.IsActive)
            {
                curriculum.IsActive = false;
                curriculum.UpdatedDate = DateTime.UtcNow;
                await _curriculumRepository.UpdateAsync(curriculum);
            }
        }

        // Query Operations
        public async Task<CurriculumDto?> GetCurriculumByIdAsync(Guid curriculumId)
        {
            var curriculum = await _curriculumRepository.GetByIdAsync(curriculumId);
            if (curriculum == null)
                return null;

            var schoolsCount = await _curriculumRepository.GetSchoolsCountAsync(curriculumId);
            var packagesCount = await _curriculumRepository.GetPackagesCountAsync(curriculumId);
            return MapToDto(curriculum, schoolsCount, packagesCount);
        }

        public async Task<List<CurriculumDto>> GetAllCurriculaAsync()
        {
            var curricula = await _curriculumRepository.GetAllAsync();
            var curriculumDtos = new List<CurriculumDto>();

            foreach (var curriculum in curricula)
            {
                var schoolsCount = await _curriculumRepository.GetSchoolsCountAsync(curriculum.CurriculumId);
                var packagesCount = await _curriculumRepository.GetPackagesCountAsync(curriculum.CurriculumId);
                curriculumDtos.Add(MapToDto(curriculum, schoolsCount, packagesCount));
            }

            return curriculumDtos;
        }

        public async Task<List<CurriculumDto>> GetActiveCurriculaAsync()
        {
            var curricula = await _curriculumRepository.GetActiveAsync();
            var curriculumDtos = new List<CurriculumDto>();

            foreach (var curriculum in curricula)
            {
                var schoolsCount = await _curriculumRepository.GetSchoolsCountAsync(curriculum.CurriculumId);
                var packagesCount = await _curriculumRepository.GetPackagesCountAsync(curriculum.CurriculumId);
                curriculumDtos.Add(MapToDto(curriculum, schoolsCount, packagesCount));
            }

            return curriculumDtos;
        }

        public async Task<List<CurriculumDto>> SearchCurriculaAsync(CurriculumSearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var curricula = await _curriculumRepository.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                curricula = curricula.Where(c => c.CurriculumName.Contains(request.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                curricula = curricula.Where(c => c.CurriculumCode.Contains(request.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.Grades))
            {
                curricula = curricula.Where(c => c.Grades.Contains(request.Grades, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (request.IsActive.HasValue)
            {
                curricula = curricula.Where(c => c.IsActive == request.IsActive.Value).ToList();
            }

            // Apply pagination
            var totalCount = curricula.Count;
            var pagedCurricula = curricula
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var curriculumDtos = new List<CurriculumDto>();
            foreach (var curriculum in pagedCurricula)
            {
                var schoolsCount = await _curriculumRepository.GetSchoolsCountAsync(curriculum.CurriculumId);
                var packagesCount = await _curriculumRepository.GetPackagesCountAsync(curriculum.CurriculumId);
                curriculumDtos.Add(MapToDto(curriculum, schoolsCount, packagesCount));
            }

            return curriculumDtos;
        }

        public async Task<int> GetCurriculaCountAsync(CurriculumSearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var curricula = await _curriculumRepository.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                curricula = curricula.Where(c => c.CurriculumName.Contains(request.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                curricula = curricula.Where(c => c.CurriculumCode.Contains(request.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.Grades))
            {
                curricula = curricula.Where(c => c.Grades.Contains(request.Grades, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (request.IsActive.HasValue)
            {
                curricula = curricula.Where(c => c.IsActive == request.IsActive.Value).ToList();
            }

            return curricula.Count;
        }

        // Related Data Operations
        public async Task<CurriculumWithSchoolsDto?> GetCurriculumWithSchoolsAsync(Guid curriculumId)
        {
            var curriculum = await _curriculumRepository.GetByIdAsync(curriculumId);
            if (curriculum == null)
                return null;

            var schools = await _curriculumRepository.GetSchoolsByCurriculumIdAsync(curriculumId);

            var curriculumWithSchools = new CurriculumWithSchoolsDto
            {
                CurriculumId = curriculum.CurriculumId,
                CurriculumCode = curriculum.CurriculumCode,
                CurriculumName = curriculum.CurriculumName,
                Grades = curriculum.Grades,
                SyllabusUrl = curriculum.SyllabusUrl,
                IsActive = curriculum.IsActive,
                TotalCredits = curriculum.TotalCredits,
                TotalSchools = schools.Count,
                TotalPackages = await _curriculumRepository.GetPackagesCountAsync(curriculumId),
                CreatedDate = curriculum.CreatedDate,
                UpdatedDate = curriculum.UpdatedDate,
                Schools = schools.Select(s => new SchoolDto
                {
                    SchoolId = s.SchoolId,
                    SchoolName = s.SchoolName,
                    CurriculumId = s.CurriculumId,
                    CurriculumName = s.Curriculum?.CurriculumName,
                    IsActive = s.IsActive,
                    TotalChildren = 0, // Would need to fetch if required, but to avoid nested loops
                    CreatedDate = s.CreatedDate,
                    UpdatedDate = s.UpdatedDate
                }).ToList()
            };

            return curriculumWithSchools;
        }

        public async Task<List<SchoolDto>> GetSchoolsByCurriculumAsync(Guid curriculumId)
        {
            var schools = await _curriculumRepository.GetSchoolsByCurriculumIdAsync(curriculumId);

            return schools.Select(s => new SchoolDto
            {
                SchoolId = s.SchoolId,
                SchoolName = s.SchoolName,
                CurriculumId = s.CurriculumId,
                CurriculumName = s.Curriculum?.CurriculumName,
                IsActive = s.IsActive,
                TotalChildren = 0,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate
            }).ToList();
        }

        // Helper Methods
        private CurriculumDto MapToDto(Curriculum curriculum, int schoolsCount, int packagesCount)
        {
            return new CurriculumDto
            {
                CurriculumId = curriculum.CurriculumId,
                CurriculumCode = curriculum.CurriculumCode,
                CurriculumName = curriculum.CurriculumName,
                Grades = curriculum.Grades,
                SyllabusUrl = curriculum.SyllabusUrl,
                IsActive = curriculum.IsActive,
                TotalCredits = curriculum.TotalCredits,
                TotalSchools = schoolsCount,
                TotalPackages = packagesCount,
                CreatedDate = curriculum.CreatedDate,
                UpdatedDate = curriculum.UpdatedDate
            };
        }
    }
}
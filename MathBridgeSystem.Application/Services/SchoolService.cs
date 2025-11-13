using MathBridgeSystem.Application.DTOs;
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
    public class SchoolService : ISchoolService
    {
        private readonly ISchoolRepository _schoolRepository;

        public SchoolService(ISchoolRepository schoolRepository)
        {
            _schoolRepository = schoolRepository ?? throw new ArgumentNullException(nameof(schoolRepository));
        }

        // CRUD Operations
        public async Task<Guid> CreateSchoolAsync(CreateSchoolRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.SchoolName))
                throw new ArgumentException("School name is required", nameof(request.SchoolName));

            // Validate curriculum exists
            var curriculum = await _schoolRepository.GetCurriculumByIdAsync(request.CurriculumId);
            if (curriculum == null)
                throw new ArgumentException($"Curriculum with ID {request.CurriculumId} not found");

            // Check for duplicate name
            var existsByName = await _schoolRepository.ExistsByNameAsync(request.SchoolName);
            if (existsByName)
                throw new ArgumentException($"School with name '{request.SchoolName}' already exists");

            var school = new School
            {
                SchoolId = Guid.NewGuid(),
                SchoolName = request.SchoolName,
                CurriculumId = request.CurriculumId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _schoolRepository.AddAsync(school);
            return school.SchoolId;
        }

        public async Task UpdateSchoolAsync(Guid schoolId, UpdateSchoolRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var school = await _schoolRepository.GetByIdAsync(schoolId);
            if (school == null)
                throw new KeyNotFoundException($"School with ID {schoolId} not found");

            bool hasChanges = false;

            // Update CurriculumId if provided
            if (request.CurriculumId.HasValue && request.CurriculumId.Value != school.CurriculumId)
            {
                var curriculum = await _schoolRepository.GetCurriculumByIdAsync(request.CurriculumId.Value);
                if (curriculum == null)
                    throw new ArgumentException($"Curriculum with ID {request.CurriculumId.Value} not found");

                school.CurriculumId = request.CurriculumId.Value;
                hasChanges = true;
            }

            // Update SchoolName if provided
            if (!string.IsNullOrWhiteSpace(request.SchoolName) && request.SchoolName != school.SchoolName)
            {
                var existsByName = await _schoolRepository.ExistsByNameAsync(request.SchoolName);
                if (existsByName)
                    throw new ArgumentException($"School with name '{request.SchoolName}' already exists");

                school.SchoolName = request.SchoolName;
                hasChanges = true;
            }

            // Update IsActive if provided
            if (request.IsActive.HasValue && request.IsActive.Value != school.IsActive)
            {
                school.IsActive = request.IsActive.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                school.UpdatedDate = DateTime.UtcNow.ToLocalTime();
                await _schoolRepository.UpdateAsync(school);
            }
        }

        public async Task DeleteSchoolAsync(Guid schoolId)
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId);
            if (school == null)
                throw new KeyNotFoundException($"School with ID {schoolId} not found");

            var childrenCount = await _schoolRepository.GetChildrenCountAsync(schoolId);
            if (childrenCount > 0)
                throw new InvalidOperationException($"Cannot delete school with ID {schoolId} because it has {childrenCount} enrolled children");

            await _schoolRepository.DeleteAsync(schoolId);
        }

        public async Task ActivateSchoolAsync(Guid schoolId)
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId);
            if (school == null)
                throw new KeyNotFoundException($"School with ID {schoolId} not found");

            if (!school.IsActive)
            {
                school.IsActive = true;
                school.UpdatedDate = DateTime.UtcNow.ToLocalTime();
                await _schoolRepository.UpdateAsync(school);
            }
        }

        public async Task DeactivateSchoolAsync(Guid schoolId)
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId);
            if (school == null)
                throw new KeyNotFoundException($"School with ID {schoolId} not found");

            if (school.IsActive)
            {
                school.IsActive = false;
                school.UpdatedDate = DateTime.UtcNow.ToLocalTime();
                await _schoolRepository.UpdateAsync(school);
            }
        }

        // Query Operations
        public async Task<SchoolDto?> GetSchoolByIdAsync(Guid schoolId)
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId);
            if (school == null)
                return null;

            var childrenCount = await _schoolRepository.GetChildrenCountAsync(schoolId);
            return MapToDto(school, childrenCount);
        }

        public async Task<List<SchoolDto>> GetAllSchoolsAsync()
        {
            var schools = await _schoolRepository.GetAllAsync();
            var schoolDtos = new List<SchoolDto>();

            foreach (var school in schools)
            {
                var childrenCount = await _schoolRepository.GetChildrenCountAsync(school.SchoolId);
                schoolDtos.Add(MapToDto(school, childrenCount));
            }

            return schoolDtos;
        }

        public async Task<List<SchoolDto>> GetActiveSchoolsAsync()
        {
            var schools = await _schoolRepository.GetActiveSchoolsAsync();
            var schoolDtos = new List<SchoolDto>();

            foreach (var school in schools)
            {
                var childrenCount = await _schoolRepository.GetChildrenCountAsync(school.SchoolId);
                schoolDtos.Add(MapToDto(school, childrenCount));
            }

            return schoolDtos;
        }

        public async Task<List<SchoolDto>> SearchSchoolsAsync(SchoolSearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var schools = await _schoolRepository.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                schools = schools.Where(s => s.SchoolName.Contains(request.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (request.CurriculumId.HasValue)
            {
                schools = schools.Where(s => s.CurriculumId == request.CurriculumId.Value).ToList();
            }

            if (request.IsActive.HasValue)
            {
                schools = schools.Where(s => s.IsActive == request.IsActive.Value).ToList();
            }

            // Apply pagination
            var totalCount = schools.Count;
            var pagedSchools = schools
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var schoolDtos = new List<SchoolDto>();
            foreach (var school in pagedSchools)
            {
                var childrenCount = await _schoolRepository.GetChildrenCountAsync(school.SchoolId);
                schoolDtos.Add(MapToDto(school, childrenCount));
            }

            return schoolDtos;
        }

        public async Task<int> GetSchoolsCountAsync(SchoolSearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var schools = await _schoolRepository.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                schools = schools.Where(s => s.SchoolName.Contains(request.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (request.CurriculumId.HasValue)
            {
                schools = schools.Where(s => s.CurriculumId == request.CurriculumId.Value).ToList();
            }

            if (request.IsActive.HasValue)
            {
                schools = schools.Where(s => s.IsActive == request.IsActive.Value).ToList();
            }

            return schools.Count;
        }

        public async Task<List<SchoolDto>> GetSchoolsByCurriculumAsync(Guid curriculumId)
        {
            var schools = await _schoolRepository.GetSchoolsByCurriculumIdAsync(curriculumId);
            var schoolDtos = new List<SchoolDto>();

            foreach (var school in schools)
            {
                var childrenCount = await _schoolRepository.GetChildrenCountAsync(school.SchoolId);
                schoolDtos.Add(MapToDto(school, childrenCount));
            }

            return schoolDtos;
        }

        // Related Data Operations
        public async Task<SchoolWithChildrenDto?> GetSchoolWithChildrenAsync(Guid schoolId)
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId);
            if (school == null)
                return null;

            var children = await _schoolRepository.GetChildrenBySchoolIdAsync(schoolId);
            
            var schoolWithChildren = new SchoolWithChildrenDto
            {
                SchoolId = school.SchoolId,
                SchoolName = school.SchoolName,
                CurriculumId = school.CurriculumId,
                CurriculumName = school.Curriculum?.CurriculumName,
                IsActive = school.IsActive,
                TotalChildren = children.Count,
                CreatedDate = school.CreatedDate,
                UpdatedDate = school.UpdatedDate,
                Children = children.Select(c => new ChildDto
                {
                    ChildId = c.ChildId,
                    FullName = c.FullName,
                    SchoolId = c.SchoolId,
                    SchoolName = c.School?.SchoolName ?? string.Empty,
                    CenterId = c.CenterId,
                    CenterName = c.Center?.Name,
                    Grade = c.Grade,
                    DateOfBirth = c.DateOfBirth,
                    Status = c.Status
                }).ToList()
            };

            return schoolWithChildren;
        }

        public async Task<List<ChildDto>> GetChildrenBySchoolAsync(Guid schoolId)
        {
            var school = await _schoolRepository.GetByIdAsync(schoolId);
            if (school == null)
                throw new KeyNotFoundException($"School with ID {schoolId} not found");

            var children = await _schoolRepository.GetChildrenBySchoolIdAsync(schoolId);
            
            return children.Select(c => new ChildDto
            {
                ChildId = c.ChildId,
                FullName = c.FullName,
                SchoolId = c.SchoolId,
                SchoolName = c.School?.SchoolName ?? string.Empty,
                CenterId = c.CenterId,
                CenterName = c.Center?.Name,
                Grade = c.Grade,
                DateOfBirth = c.DateOfBirth,
                Status = c.Status
            }).ToList();
        }

        // Helper Methods
        private SchoolDto MapToDto(School school, int childrenCount)
        {
            return new SchoolDto
            {
                SchoolId = school.SchoolId,
                SchoolName = school.SchoolName,
                CurriculumId = school.CurriculumId,
                CurriculumName = school.Curriculum?.CurriculumName,
                IsActive = school.IsActive,
                TotalChildren = childrenCount,
                CreatedDate = school.CreatedDate,
                UpdatedDate = school.UpdatedDate
            };
        }
    }
}
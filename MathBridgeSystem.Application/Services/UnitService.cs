using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class UnitService : IUnitService
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ICurriculumRepository _curriculumRepository;

        public UnitService(IUnitRepository unitRepository, ICurriculumRepository curriculumRepository)
        {
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            _curriculumRepository = curriculumRepository ?? throw new ArgumentNullException(nameof(curriculumRepository));
        }

        public async Task<Guid> CreateUnitAsync(CreateUnitRequest request, Guid? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(request.UnitName))
                throw new ArgumentException("Unit name is required.");

            // Check if curriculum exists
            if (!await _curriculumRepository.ExistsAsync(request.CurriculumId))
                throw new InvalidOperationException("Curriculum not found.");

            // Check if unit already exists in this curriculum
            if (await _unitRepository.ExistsByNameAsync(request.UnitName, request.CurriculumId))
                throw new InvalidOperationException($"Unit with name '{request.UnitName}' already exists in this curriculum.");

            // Auto-assign unit order if not provided
            int unitOrder = request.UnitOrder ?? await _unitRepository.GetMaxUnitOrderAsync(request.CurriculumId) + 1;

            var unit = new Unit
            {
                UnitId = Guid.NewGuid(),
                CurriculumId = request.CurriculumId,
                UnitName = request.UnitName.Trim(),
                UnitDescription = request.UnitDescription?.Trim(),
                UnitOrder = unitOrder,
                Credit = request.Credit,
                LearningObjectives = request.LearningObjectives?.Trim(),
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = createdBy
            };

            await _unitRepository.AddAsync(unit);
            return unit.UnitId;
        }

        public async Task UpdateUnitAsync(Guid id, UpdateUnitRequest request, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(request.UnitName))
                throw new ArgumentException("Unit name is required.");

            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null)
                throw new InvalidOperationException("Unit not found.");

            // Check if new unit name already exists in the same curriculum (excluding current unit)
            var existingUnit = await _unitRepository.GetByNameAsync(request.UnitName);
            if (existingUnit != null && existingUnit.UnitId != id && existingUnit.CurriculumId == unit.CurriculumId)
                throw new InvalidOperationException($"Unit with name '{request.UnitName}' already exists in this curriculum.");

            unit.UnitName = request.UnitName.Trim();
            unit.UnitDescription = request.UnitDescription?.Trim();
            unit.UnitOrder = request.UnitOrder;
            unit.Credit = request.Credit;
            unit.LearningObjectives = request.LearningObjectives?.Trim();
            unit.IsActive = request.IsActive;
            unit.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            unit.UpdatedBy = updatedBy;

            await _unitRepository.UpdateAsync(unit);
        }

        public async Task DeleteUnitAsync(Guid id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null)
                throw new InvalidOperationException("Unit not found.");

            await _unitRepository.DeleteAsync(id);
        }

        public async Task<UnitDto?> GetUnitByIdAsync(Guid id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null)
                return null;

            return MapToDto(unit);
        }

        public async Task<List<UnitDto>> GetAllUnitsAsync()
        {
            var units = await _unitRepository.GetAllAsync();
            return units.Select(MapToDto).ToList();
        }

        public async Task<List<UnitDto>> GetUnitsByCurriculumIdAsync(Guid curriculumId)
        {
            var units = await _unitRepository.GetByCurriculumIdAsync(curriculumId);
            return units.Select(MapToDto).ToList();
        }

        public async Task<UnitDto?> GetUnitByNameAsync(string unitName)
        {
            var unit = await _unitRepository.GetByNameAsync(unitName);
            if (unit == null)
                return null;

            return MapToDto(unit);
        }

        public async Task<List<UnitDto>> GetUnitsByContractIdAsync(Guid contractId)
        {
            var units = await _unitRepository.GetByContractIdAsync(contractId);
            return units.Select(MapToDto).ToList();
        }

        private UnitDto MapToDto(Unit unit)
        {
            return new UnitDto
            {
                UnitId = unit.UnitId,
                CurriculumId = unit.CurriculumId == null ? Guid.Empty : unit.CurriculumId.Value,
                CurriculumName = unit.Curriculum?.CurriculumName ?? string.Empty,
                UnitName = unit.UnitName,
                UnitDescription = unit.UnitDescription,
                UnitOrder = unit.UnitOrder,
                Credit = unit.Credit,
                LearningObjectives = unit.LearningObjectives,
                IsActive = unit.IsActive,
                CreatedDate = unit.CreatedDate,
                CreatedByName = unit.CreatedByNavigation?.FullName,
                UpdatedDate = unit.UpdatedDate,
                UpdatedByName = unit.UpdatedByNavigation?.FullName
            };
        }
    }
}
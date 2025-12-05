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
    public class MathConceptService : IMathConceptService
    {
        private readonly IMathConceptRepository _mathConceptRepository;
        private readonly IUnitRepository _unitRepository;

        public MathConceptService(
            IMathConceptRepository mathConceptRepository,
            IUnitRepository unitRepository)
        {
            _mathConceptRepository = mathConceptRepository ?? throw new ArgumentNullException(nameof(mathConceptRepository));
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
        }

        public async Task<Guid> CreateMathConceptAsync(CreateMathConceptRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Math concept name is required.");

                // Check if math concept already exists
                if (await _mathConceptRepository.ExistsByNameAsync(request.Name))
                    throw new InvalidOperationException($"Math concept with name '{request.Name}' already exists.");

                var mathConcept = new MathConcept
                {
                    ConceptId = Guid.NewGuid(),
                    Name = request.Name.Trim(),
                    Category = request.Category?.Trim()
                };

                await _mathConceptRepository.AddAsync(mathConcept);

                // Link to units if provided
                if (request.UnitIds != null && request.UnitIds.Count > 0)
                {
                    await LinkMathConceptToUnitsInternalAsync(mathConcept.ConceptId, request.UnitIds);
                }

                return mathConcept.ConceptId;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error creating math concept.", ex);
            }
        }

        public async Task UpdateMathConceptAsync(Guid id, UpdateMathConceptRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Math concept name is required.");

                var mathConcept = await _mathConceptRepository.GetByIdAsync(id);
                if (mathConcept == null)
                    throw new InvalidOperationException("Math concept not found.");

                // Check if new name already exists (excluding current concept)
                var existingConcept = await _mathConceptRepository.GetByNameAsync(request.Name);
                if (existingConcept != null && existingConcept.ConceptId != id)
                    throw new InvalidOperationException($"Math concept with name '{request.Name}' already exists.");

                mathConcept.Name = request.Name.Trim();
                mathConcept.Category = request.Category?.Trim();

                await _mathConceptRepository.UpdateAsync(mathConcept);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error updating math concept.", ex);
            }
        }

        public async Task DeleteMathConceptAsync(Guid id)
        {
            try
            {
                var mathConcept = await _mathConceptRepository.GetByIdAsync(id);
                if (mathConcept == null)
                    throw new InvalidOperationException("Math concept not found.");

                // Check if linked to any units
                if (await _mathConceptRepository.IsMathConceptLinkedToUnitsAsync(id))
                    throw new InvalidOperationException("Cannot delete math concept because it is linked to one or more units. Please unlink them first.");

                await _mathConceptRepository.DeleteAsync(id);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting math concept.", ex);
            }
        }

        public async Task<MathConceptDto?> GetMathConceptByIdAsync(Guid id)
        {
            try
            {
                var mathConcept = await _mathConceptRepository.GetByIdAsync(id);
                if (mathConcept == null)
                    return null;

                return MapToDto(mathConcept);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concept.", ex);
            }
        }

        public async Task<List<MathConceptDto>> GetAllMathConceptsAsync()
        {
            try
            {
                var mathConcepts = await _mathConceptRepository.GetAllAsync();
                return mathConcepts.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving all math concepts.", ex);
            }
        }

        public async Task<List<MathConceptDto>> GetMathConceptsByUnitIdAsync(Guid unitId)
        {
            try
            {
                if (!await _unitRepository.ExistsAsync(unitId))
                    throw new InvalidOperationException("Unit not found.");

                var mathConcepts = await _mathConceptRepository.GetByUnitIdAsync(unitId);
                return mathConcepts.Select(MapToDto).ToList();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concepts by unit ID.", ex);
            }
        }

        public async Task<MathConceptDto?> GetMathConceptByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name cannot be empty.");

                var mathConcept = await _mathConceptRepository.GetByNameAsync(name);
                if (mathConcept == null)
                    return null;

                return MapToDto(mathConcept);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concept by name.", ex);
            }
        }

        public async Task<List<MathConceptDto>> GetMathConceptsByCategoryAsync(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    throw new ArgumentException("Category cannot be empty.");

                var mathConcepts = await _mathConceptRepository.GetByCategoryAsync(category);
                return mathConcepts.Select(MapToDto).ToList();
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concepts by category.", ex);
            }
        }

        public async Task LinkMathConceptToUnitsAsync(Guid conceptId, List<Guid> unitIds)
        {
            try
            {
                await LinkMathConceptToUnitsInternalAsync(conceptId, unitIds);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error linking math concept to units.", ex);
            }
        }

        public async Task UnlinkMathConceptFromUnitsAsync(Guid conceptId, List<Guid> unitIds)
        {
            try
            {
                if (unitIds == null || unitIds.Count == 0)
                    throw new ArgumentException("At least one unit ID is required.");

                var mathConcept = await _mathConceptRepository.GetByIdAsync(conceptId);
                if (mathConcept == null)
                    throw new InvalidOperationException("Math concept not found.");

                foreach (var unitId in unitIds)
                {
                    var unit = await _unitRepository.GetByIdAsync(unitId);
                    if (unit == null)
                        throw new InvalidOperationException($"Unit with ID {unitId} not found.");

                    // Remove the unit from the concept's Units collection
                    var unitToRemove = mathConcept.Units.FirstOrDefault(u => u.UnitId == unitId);
                    if (unitToRemove != null)
                    {
                        mathConcept.Units.Remove(unitToRemove);
                    }
                }

                await _mathConceptRepository.UpdateAsync(mathConcept);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error unlinking math concept from units.", ex);
            }
        }

        private async Task LinkMathConceptToUnitsInternalAsync(Guid conceptId, List<Guid> unitIds)
        {
            if (unitIds == null || unitIds.Count == 0)
                throw new ArgumentException("At least one unit ID is required.");

            var mathConcept = await _mathConceptRepository.GetByIdAsync(conceptId);
            if (mathConcept == null)
                throw new InvalidOperationException("Math concept not found.");

            foreach (var unitId in unitIds)
            {
                var unit = await _unitRepository.GetByIdAsync(unitId);
                if (unit == null)
                    throw new InvalidOperationException($"Unit with ID {unitId} not found.");

                // Check if already linked
                if (!mathConcept.Units.Any(u => u.UnitId == unitId))
                {
                    mathConcept.Units.Add(unit);
                }
            }

            await _mathConceptRepository.UpdateAsync(mathConcept);
        }

        private MathConceptDto MapToDto(MathConcept mathConcept)
        {
            return new MathConceptDto
            {
                ConceptId = mathConcept.ConceptId,
                Name = mathConcept.Name,
                Category = mathConcept.Category,
                LinkedUnits = mathConcept.Units?.Select(u => new UnitSummaryDto
                {
                    UnitId = u.UnitId,
                    UnitName = u.UnitName,
                    CurriculumName = u.Curriculum?.CurriculumName,
                    UnitOrder = u.UnitOrder
                }).ToList()
            };
        }
    }
}

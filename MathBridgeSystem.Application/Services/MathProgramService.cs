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
    public class MathProgramService : IMathProgramService
    {
        private readonly IMathProgramRepository _mathProgramRepository;

        public MathProgramService(IMathProgramRepository mathProgramRepository)
        {
            _mathProgramRepository = mathProgramRepository ?? throw new ArgumentNullException(nameof(mathProgramRepository));
        }

        public async Task<Guid> CreateMathProgramAsync(CreateMathProgramRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProgramName))
                throw new ArgumentException("Program name is required.");

            // Check if program already exists
            if (await _mathProgramRepository.ExistsByNameAsync(request.ProgramName))
                throw new InvalidOperationException($"Math program with name '{request.ProgramName}' already exists.");

            var mathProgram = new MathProgram
            {
                ProgramId = Guid.NewGuid(),
                ProgramName = request.ProgramName.Trim(),
                Description = request.Description?.Trim(),
                LinkSyllabus = request.LinkSyllabus?.Trim()
            };

            await _mathProgramRepository.AddAsync(mathProgram);
            return mathProgram.ProgramId;
        }

        public async Task UpdateMathProgramAsync(Guid id, UpdateMathProgramRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProgramName))
                throw new ArgumentException("Program name is required.");

            var mathProgram = await _mathProgramRepository.GetByIdAsync(id);
            if (mathProgram == null)
                throw new InvalidOperationException("Math program not found.");

            // Check if new program name already exists (excluding current program)
            var existingProgram = await _mathProgramRepository.GetByNameAsync(request.ProgramName);
            if (existingProgram != null && existingProgram.ProgramId != id)
                throw new InvalidOperationException($"Math program with name '{request.ProgramName}' already exists.");

            mathProgram.ProgramName = request.ProgramName.Trim();
            mathProgram.Description = request.Description?.Trim();
            mathProgram.LinkSyllabus = request.LinkSyllabus?.Trim();

            await _mathProgramRepository.UpdateAsync(mathProgram);
        }

        public async Task DeleteMathProgramAsync(Guid id)
        {
            var mathProgram = await _mathProgramRepository.GetByIdAsync(id);
            if (mathProgram == null)
                throw new InvalidOperationException("Math program not found.");

            // Check if program has associated packages or test results
            if (mathProgram.PaymentPackages != null && mathProgram.PaymentPackages.Any())
                throw new InvalidOperationException("Cannot delete math program with associated payment packages.");

            if (mathProgram.TestResults != null && mathProgram.TestResults.Any())
                throw new InvalidOperationException("Cannot delete math program with associated test results.");

            await _mathProgramRepository.DeleteAsync(id);
        }

        public async Task<MathProgramDto?> GetMathProgramByIdAsync(Guid id)
        {
            var mathProgram = await _mathProgramRepository.GetByIdAsync(id);
            if (mathProgram == null)
                return null;

            return new MathProgramDto
            {
                ProgramId = mathProgram.ProgramId,
                ProgramName = mathProgram.ProgramName,
                Description = mathProgram.Description,
                LinkSyllabus = mathProgram.LinkSyllabus,
                PackageCount = mathProgram.PaymentPackages?.Count ?? 0,
                TestResultCount = mathProgram.TestResults?.Count ?? 0
            };
        }

        public async Task<List<MathProgramDto>> GetAllMathProgramsAsync()
        {
            var mathPrograms = await _mathProgramRepository.GetAllAsync();
            return mathPrograms.Select(mp => new MathProgramDto
            {
                ProgramId = mp.ProgramId,
                ProgramName = mp.ProgramName,
                Description = mp.Description,
                LinkSyllabus = mp.LinkSyllabus,
                PackageCount = mp.PaymentPackages?.Count ?? 0,
                TestResultCount = mp.TestResults?.Count ?? 0
            }).ToList();
        }

        public async Task<MathProgramDto?> GetMathProgramByNameAsync(string programName)
        {
            var mathProgram = await _mathProgramRepository.GetByNameAsync(programName);
            if (mathProgram == null)
                return null;

            return new MathProgramDto
            {
                ProgramId = mathProgram.ProgramId,
                ProgramName = mathProgram.ProgramName,
                Description = mathProgram.Description,
                LinkSyllabus = mathProgram.LinkSyllabus,
                PackageCount = mathProgram.PaymentPackages?.Count ?? 0,
                TestResultCount = mathProgram.TestResults?.Count ?? 0
            };
        }
    }
}
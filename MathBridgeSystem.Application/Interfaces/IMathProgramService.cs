using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IMathProgramService
    {
        Task<Guid> CreateMathProgramAsync(CreateMathProgramRequest request);
        Task UpdateMathProgramAsync(Guid id, UpdateMathProgramRequest request);
        Task DeleteMathProgramAsync(Guid id);
        Task<MathProgramDto?> GetMathProgramByIdAsync(Guid id);
        Task<List<MathProgramDto>> GetAllMathProgramsAsync();
        Task<MathProgramDto?> GetMathProgramByNameAsync(string programName);
    }
}
using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IMathProgramRepository
    {
        Task AddAsync(MathProgram mathProgram);
        Task UpdateAsync(MathProgram mathProgram);
        Task DeleteAsync(Guid id);
        Task<MathProgram?> GetByIdAsync(Guid id);
        Task<List<MathProgram>> GetAllAsync();
        Task<MathProgram?> GetByNameAsync(string programName);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByNameAsync(string programName);
    }
}

using MathBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Domain.Interfaces
{
    public interface IMathProgramRepository
    {
        Task<MathProgram> GetByIdAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}

using MathBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridge.Domain.Interfaces
{
    public interface ISchoolRepository
    {
        Task AddAsync(School school);
        Task UpdateAsync(School school);
        Task<School?> GetByIdAsync(Guid id);
        Task<IEnumerable<School>> GetAllAsync();
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
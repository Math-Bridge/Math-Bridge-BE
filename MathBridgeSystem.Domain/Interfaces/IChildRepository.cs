using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IChildRepository
    {
        Task AddAsync(Child child);
        Task UpdateAsync(Child child);
        Task<Child> GetByIdAsync(Guid id);
        Task<List<Child>> GetByParentIdAsync(Guid parentId);
        Task<List<Child>> GetAllAsync();
        Task<List<Contract>> GetContractsByChildIdAsync(Guid childId);
        Task<Center?> GetCenterByIdAsync(Guid centerId);
    }
}

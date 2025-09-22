using MathBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Domain.Interfaces
{
    public interface IContractRepository
    {
        Task AddAsync(Contract contract);
        Task UpdateAsync(Contract contract);
        Task<List<Contract>> GetByParentIdAsync(Guid parentId);
        Task<List<Contract>> GetByChildIdAsync(Guid childId);
        Task<Contract?> GetByIdAsync(Guid id);
        Task<List<Contract>> GetByCenterIdAsync(Guid centerId);
    }
}

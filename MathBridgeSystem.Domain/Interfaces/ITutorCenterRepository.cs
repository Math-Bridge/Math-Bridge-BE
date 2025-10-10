using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ITutorCenterRepository
    {
        Task AddAsync(TutorCenter tutorCenter);
        Task RemoveAsync(Guid tutorCenterId);
        Task<List<TutorCenter>> GetByTutorIdAsync(Guid tutorId);
        Task<List<TutorCenter>> GetByCenterIdAsync(Guid centerId);
        Task<bool> TutorIsAssignedToCenterAsync(Guid tutorId, Guid centerId);
    }
}

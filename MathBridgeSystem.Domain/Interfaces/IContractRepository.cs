using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IContractRepository
    {
        Task AddAsync(Contract contract);
        Task UpdateAsync(Contract contract);
        Task<List<Contract>> GetByParentIdAsync(Guid parentId);
        Task<Contract?> GetByIdAsync(Guid id);
        Task<Contract?> GetByIdWithPackageAsync(Guid contractId);
        Task<List<Contract>> GetAllWithDetailsAsync();
        Task<List<Contract>> GetByParentPhoneNumberAsync(string phoneNumber);
        Task<List<AvailableTutorResponse>> GetAvailableTutorsForContractAsync(int contractId);
    }

        
}
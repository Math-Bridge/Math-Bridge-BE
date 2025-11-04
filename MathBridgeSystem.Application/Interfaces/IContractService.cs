using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IContractService
    {
        Task<Guid> CreateContractAsync(CreateContractRequest request);
        Task<List<ContractDto>> GetContractsByParentAsync(Guid parentId);
        Task<bool> UpdateContractStatusAsync(Guid contractId, UpdateContractStatusRequest request, Guid staffId);
        Task<bool> AssignTutorsAsync(Guid contractId, AssignTutorToContractRequest request, Guid staffId);
    }
}
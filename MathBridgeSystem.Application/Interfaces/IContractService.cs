using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.Contract;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IContractService
    {
        Task<Guid> CreateContractAsync(CreateContractRequest request);
        Task<List<ContractDto>> GetContractsByParentAsync(Guid parentId);
        Task<ContractDto> GetContractByIdAsync(Guid contractId);
        Task<List<ContractDto>> GetAllContractsAsync();
        Task<bool> UpdateContractStatusAsync(Guid contractId, UpdateContractStatusRequest request, Guid staffId);
        Task<bool> AssignTutorsAsync(Guid contractId, AssignTutorToContractRequest request, Guid staffId);
        Task<bool> CompleteContractAsync(Guid contractId, Guid staffId);
        Task<List<ContractDto>> GetContractsByParentPhoneAsync(string phoneNumber);
        Task<List<AvailableTutorResponse>> GetAvailableTutorsAsync(Guid contractId, bool sortByRating = false, bool sortByDistance = false);
    }
}
using MathBridge.Application.DTOs;
using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Application.Interfaces
{
    public interface IContractService
    {
        Task<Guid> CreateContractAsync(CreateContractRequest request);
        Task<List<ContractDto>> GetContractsByParentAsync(Guid parentId);
        //Task<bool> ValidateLocationAsync(ValidateLocationRequest request);
    }
}

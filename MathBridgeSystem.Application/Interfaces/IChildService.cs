using MathBridge.Application.DTOs;
using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Application.Interfaces
{
    public interface IChildService
    {
        Task<Guid> AddChildAsync(Guid parentId, AddChildRequest request);
        Task UpdateChildAsync(Guid id, UpdateChildRequest request);
        Task SoftDeleteChildAsync(Guid id);
        Task RestoreChildAsync(Guid id);
        Task<ChildDto> GetChildByIdAsync(Guid id);
        Task<List<ChildDto>> GetChildrenByParentAsync(Guid parentId);
        Task<List<ChildDto>> GetAllChildrenAsync();
        Task LinkCenterAsync(Guid childId, LinkCenterRequest request);
        Task<List<ContractDto>> GetChildContractsAsync(Guid childId);
    }
}

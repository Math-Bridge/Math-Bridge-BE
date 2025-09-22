using MathBridge.Application.DTOs;
using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Application.Interfaces
{
    public interface IPackageService
    {
        Task<Guid> CreatePackageAsync(CreatePackageRequest request);
        Task<List<PaymentPackageDto>> GetAllPackagesAsync();
    }
}

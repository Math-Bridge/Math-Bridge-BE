﻿using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IPackageService
    {
        Task<Guid> CreatePackageAsync(CreatePackageRequest request);
        Task<List<PaymentPackageDto>> GetAllPackagesAsync();
        Task<PaymentPackageDto> UpdatePackageAsync(Guid id, UpdatePackageRequest request);
        Task DeletePackageAsync(Guid id);
        public Task<PaymentPackageDto> GetPackageByIdAsync(Guid id);
        Task<List<PaymentPackageDto>> GetAllActivePackagesAsync();
        Task<PaymentPackageDto> GetActivePackageByIdAsync(Guid id);
        Task<string> UploadPackageImageAsync(Guid packageId, IFormFile file);
    }
}

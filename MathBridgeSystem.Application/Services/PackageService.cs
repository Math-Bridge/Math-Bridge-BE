using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class PackageService : IPackageService
    {
        private readonly IPackageRepository _packageRepository;

        public PackageService(IPackageRepository packageRepository)
        {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        public async Task<Guid> CreatePackageAsync(CreatePackageRequest request)
        {            if (!new[] { "grade 9", "grade 10", "grade 11", "grade 12" }.Contains(request.Grade))
                throw new Exception("Invalid grade");

            var package = new PaymentPackage
            {
                PackageId = Guid.NewGuid(),
                PackageName = request.PackageName,
                Grade = request.Grade,
                Price = request.Price,
                SessionCount = request.SessionCount,
                SessionsPerWeek = request.SessionsPerWeek,
                MaxReschedule = request.MaxReschedule,
                DurationDays = request.DurationDays,
                Description = request.Description,
                CreatedDate = DateTime.UtcNow
            };

            await _packageRepository.AddAsync(package);
            return package.PackageId;
        }

        public async Task<List<PaymentPackageDto>> GetAllPackagesAsync()
        {
            var packages = await _packageRepository.GetAllAsync();
            return packages.Select(p => new PaymentPackageDto
            {
                PackageId = p.PackageId,
                PackageName = p.PackageName,
                Grade = p.Grade,
                Price = p.Price,
                SessionCount = p.SessionCount,
                SessionsPerWeek = p.SessionsPerWeek,
                MaxReschedule = p.MaxReschedule,
                DurationDays = p.DurationDays,
                Description = p.Description
            }).ToList();
        }
    }
}
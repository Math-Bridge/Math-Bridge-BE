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
        private readonly IMathProgramRepository _programRepository;

        public PackageService(IPackageRepository packageRepository, IMathProgramRepository programRepository)
        {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
            _programRepository = programRepository ?? throw new ArgumentNullException(nameof(programRepository));
        }

        public async Task<Guid> CreatePackageAsync(CreatePackageRequest request)
        {
            var program = await _programRepository.GetByIdAsync(request.ProgramId);
            if (program == null)
                throw new Exception("Program not found");

            if (!new[] { "grade 9", "grade 10", "grade 11", "grade 12" }.Contains(request.Grade))
                throw new Exception("Invalid grade");

            var package = new PaymentPackage
            {
                PackageId = Guid.NewGuid(),
                PackageName = request.PackageName,
                ProgramId = request.ProgramId,
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
                ProgramId = p.ProgramId,
                ProgramName = p.Program.ProgramName,
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
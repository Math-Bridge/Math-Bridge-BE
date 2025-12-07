using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace MathBridgeSystem.Application.Services
{
    public class PackageService : IPackageService
    {
        private readonly IPackageRepository _packageRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly string[] _validGrades = { "grade 9", "grade 10", "grade 11", "grade 12" };

        public PackageService(IPackageRepository packageRepository, ICloudinaryService cloudinaryService)
        {
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
        }

        public async Task<Guid> CreatePackageAsync(CreatePackageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PackageName))
                throw new ArgumentException("Package name is required.");

            if (!_validGrades.Contains(request.Grade.ToLower()))
                throw new ArgumentException("Invalid grade. Must be grade 9, 10, 11, or 12.");

            if (request.Price <= 0)
                throw new ArgumentException("Price must be greater than 0.");
            if(request.SessionsPerWeek < 3)
                throw new ArgumentException("Sessions per week must be at least 3.");

            // Kiểm tra Curriculum tồn tại
            var curriculumExists = await _packageRepository.ExistsCurriculumAsync(request.CurriculumId);
            if (!curriculumExists)
                throw new KeyNotFoundException("Curriculum not found.");

            var package = new PaymentPackage
            {
                PackageId = Guid.NewGuid(),
                PackageName = request.PackageName,
                Grade = request.Grade,
                Price = request.Price,
                SessionCount = request.SessionCount, // 3 months
                SessionsPerWeek = request.SessionsPerWeek,
                MaxReschedule = request.MaxReschedule,
                DurationDays = request.DurationDays,
                Description = request.Description,
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                CurriculumId = request.CurriculumId,
                IsActive = request.IsActive,
                ImageUrl = request.ImageUrl,
                ImageVersion = request.ImageVersion
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
                Description = p.Description,
                IsActive = p.IsActive,
                CurriculumId = p.CurriculumId,
                ImageUrl = p.ImageUrl,
                ImageVersion = p.ImageVersion
            }).ToList();
        }

        public async Task<PaymentPackageDto> GetPackageByIdAsync(Guid id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            return new PaymentPackageDto
            {
                PackageId = package.PackageId,
                PackageName = package.PackageName,
                Grade = package.Grade,
                Price = package.Price,
                SessionCount = package.SessionCount,
                SessionsPerWeek = package.SessionsPerWeek,
                MaxReschedule = package.MaxReschedule,
                DurationDays = package.DurationDays,
                Description = package.Description,
                IsActive = package.IsActive,
                CurriculumId = package.CurriculumId,
                ImageUrl = package.ImageUrl,
                ImageVersion = package.ImageVersion
            };
        }

        public async Task<PaymentPackageDto> UpdatePackageAsync(Guid id, UpdatePackageRequest request)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
                throw new KeyNotFoundException("Package not found.");

            if (!string.IsNullOrWhiteSpace(request.Grade) &&
                !_validGrades.Contains(request.Grade.ToLower()))
                throw new ArgumentException("Invalid grade.");

            if (request.Price.HasValue && request.Price <= 0)
                throw new ArgumentException("Price must be greater than 0.");
            if(request.SessionsPerWeek < 3)
                throw new ArgumentException("Sessions per week must be at least 3.");

            if (request.CurriculumId.HasValue)
            {
                var exists = await _packageRepository.ExistsCurriculumAsync(request.CurriculumId.Value);
                if (!exists)
                    throw new KeyNotFoundException("Curriculum not found.");
            }

            // Cập nhật từng field nếu có
            if (!string.IsNullOrWhiteSpace(request.PackageName))
                package.PackageName = request.PackageName;

            if (!string.IsNullOrWhiteSpace(request.Grade))
                package.Grade = request.Grade;

            if (request.Price.HasValue) package.Price = request.Price.Value;
            if (request.SessionsPerWeek.HasValue) package.SessionsPerWeek = request.SessionsPerWeek.Value;
            if (request.SessionCount.HasValue) package.SessionCount = request.SessionCount.Value;
            if (request.MaxReschedule.HasValue) package.MaxReschedule = request.MaxReschedule.Value;
            if (request.DurationDays.HasValue) package.DurationDays = request.DurationDays.Value;
            if (request.Description != null) package.Description = request.Description;
            if (request.CurriculumId.HasValue) package.CurriculumId = request.CurriculumId.Value;
            if (request.IsActive.HasValue) package.IsActive = request.IsActive.Value;
            if (request.ImageUrl != null) package.ImageUrl = request.ImageUrl;
            if (request.ImageVersion.HasValue) package.ImageVersion = request.ImageVersion.Value;

            package.UpdatedDate = DateTime.UtcNow.ToLocalTime();

            await _packageRepository.UpdateAsync(package);

            return new PaymentPackageDto
            {
                PackageId = package.PackageId,
                PackageName = package.PackageName,
                Grade = package.Grade,
                Price = package.Price,
                SessionCount = package.SessionCount,
                SessionsPerWeek = package.SessionsPerWeek,
                MaxReschedule = package.MaxReschedule,
                DurationDays = package.DurationDays,
                Description = package.Description,
                IsActive = package.IsActive,
                ImageUrl = package.ImageUrl,
                ImageVersion = package.ImageVersion
            };
        }

        public async Task DeletePackageAsync(Guid id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
                throw new KeyNotFoundException("Package not found.");

            var inUse = await _packageRepository.IsPackageInUseAsync(id);
            if (inUse)
                throw new InvalidOperationException("Cannot delete package because it is used in one or more contracts.");

            await _packageRepository.DeleteAsync(id);
        }

        public async Task<List<PaymentPackageDto>> GetAllActivePackagesAsync()
        {
            var packages = await _packageRepository.GetAllActivePackagesAsync();
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
                Description = p.Description,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl,
                ImageVersion = p.ImageVersion
            }).ToList();
        }

        public async Task<PaymentPackageDto> GetActivePackageByIdAsync(Guid id)
        {
            var package = await _packageRepository.GetActivePackageByIdAsync(id);
            if (package == null)
                return null;

            return new PaymentPackageDto
            {
                PackageId = package.PackageId,
                PackageName = package.PackageName,
                Grade = package.Grade,
                Price = package.Price,
                SessionCount = package.SessionCount,
                SessionsPerWeek = package.SessionsPerWeek,
                MaxReschedule = package.MaxReschedule,
                DurationDays = package.DurationDays,
                Description = package.Description,
                IsActive = package.IsActive,
                ImageUrl = package.ImageUrl,
                ImageVersion = package.ImageVersion
            };
        }

        public async Task<string> UploadPackageImageAsync(Guid packageId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("No file uploaded or file is empty.");

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException("Invalid file type. Only JPG, PNG, and WebP are allowed.");

                // Validate file size (2MB limit)
                if (file.Length > 2 * 1024 * 1024)
                    throw new ArgumentException("File size exceeds 2MB limit.");

                // Get package
                var package = await _packageRepository.GetByIdAsync(packageId);
                if (package == null)
                    throw new KeyNotFoundException($"Package with ID {packageId} not found.");

                // Upload to Cloudinary
                string imageUrl = await _cloudinaryService.UploadAvatarAsync(file, packageId);

                // Update package with new image URL and increment version
                package.ImageUrl = imageUrl;
                package.ImageVersion = (byte)((package.ImageVersion ?? 0) + 1);
                package.UpdatedDate = DateTime.UtcNow.ToLocalTime();

                await _packageRepository.UpdateAsync(package);

                return imageUrl;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload package image: {ex.Message}", ex);
            }
        }
    }
}
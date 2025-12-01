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
    public class ChildService : IChildService
    {
        private readonly IChildRepository _childRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public ChildService(
            IChildRepository childRepository,
            IUserRepository userRepository,
            ICenterRepository centerRepository,
            ICloudinaryService cloudinaryService)
        {
            _childRepository = childRepository ?? throw new ArgumentNullException(nameof(childRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _centerRepository = centerRepository ?? throw new ArgumentNullException(nameof(centerRepository));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
        }

        public async Task<Guid> AddChildAsync(Guid parentId, AddChildRequest request)
        {
            var parent = await _userRepository.GetByIdAsync(parentId);
            if (parent == null || parent.RoleId != 3) // Assuming 3 is 'parent'
                throw new Exception("Invalid parent");

            // Check if child with same parent and name already exists
            var existingChildren = await _childRepository.GetByParentIdAsync(parentId);
            if (existingChildren.Any(c => c.FullName.Equals(request.FullName, StringComparison.OrdinalIgnoreCase) && c.Status != "deleted"))
                throw new Exception("A child with the same name already exists for this parent");

            // Validate center if provided
            if (request.CenterId.HasValue)
            {
                var center = await _centerRepository.GetByIdAsync(request.CenterId.Value);
                if (center == null)
                    throw new Exception("Center not found");
            }

            if (!new[] { "grade 9", "grade 10", "grade 11", "grade 12" }.Contains(request.Grade))
                throw new Exception("Invalid grade");
            
            var child = new Child
            {
                ChildId = Guid.NewGuid(),
                ParentId = parentId,
                FullName = request.FullName,
                SchoolId = request.SchoolId,
                CenterId = request.CenterId, // Có thể NULL
                Grade = request.Grade,
                DateOfBirth = request.DateOfBirth,
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                Status = "active"
            };

            await _childRepository.AddAsync(child);
            return child.ChildId;
        }

        public async Task UpdateChildAsync(Guid id, UpdateChildRequest request)
        {
            var child = await _childRepository.GetByIdAsync(id);
            if (child == null || child.Status == "deleted")
                throw new Exception("Child not found or deleted");

                        // Validate center if provided
            if (request.CenterId.HasValue)
            {
                var center = await _centerRepository.GetByIdAsync(request.CenterId.Value);
                if (center == null)
                    throw new Exception("Center not found");
            }

            if (!new[] { "grade 9", "grade 10", "grade 11", "grade 12" }.Contains(request.Grade))
                throw new Exception("Invalid grade");

            child.FullName = request.FullName;
            child.SchoolId = request.SchoolId;
            child.CenterId = request.CenterId; // Added
            child.Grade = request.Grade;
            child.DateOfBirth = request.DateOfBirth;

            await _childRepository.UpdateAsync(child);
        }

        public async Task SoftDeleteChildAsync(Guid id)
        {
            var child = await _childRepository.GetByIdAsync(id);
            if (child == null || child.Status == "deleted")
                throw new Exception("Child not found or already deleted");

            child.Status = "deleted";
            await _childRepository.UpdateAsync(child);
        }

        public async Task RestoreChildAsync(Guid id)
        {
            var child = await _childRepository.GetByIdAsync(id);
            if (child == null || child.Status == "active")
                throw new Exception("Child not found or not deleted");

            child.Status = "active";
            await _childRepository.UpdateAsync(child);
        }

        public async Task<ChildDto> GetChildByIdAsync(Guid id)
        {
            var child = await _childRepository.GetByIdAsync(id);
            if (child == null)
                throw new Exception("Child not found");

            var centerName = child.Center != null ? child.Center.Name : null;

            return new ChildDto
            {
                ChildId = child.ChildId,
                FullName = child.FullName,
                SchoolId = child.SchoolId,
                SchoolName = child.School?.SchoolName ?? string.Empty,
                CenterId = child.CenterId,
                CenterName = centerName,
                Grade = child.Grade,
                DateOfBirth = child.DateOfBirth,
                Status = child.Status,
                AvatarUrl = child.AvatarUrl,
                AvatarVersion = child.AvatarVersion
            };
        }

        public async Task<List<ChildDto>> GetChildrenByParentAsync(Guid parentId)
        {
            var children = await _childRepository.GetByParentIdAsync(parentId);
            return children.Select(c => new ChildDto
                {
                    ChildId = c.ChildId,
                    FullName = c.FullName,
                    SchoolId = c.SchoolId,
                    SchoolName = c.School?.SchoolName ?? string.Empty,
                    CenterId = c.CenterId,
                    CenterName = c.Center?.Name,
                    Grade = c.Grade,
                    DateOfBirth = c.DateOfBirth,
                    Status = c.Status,
                    AvatarUrl = c.AvatarUrl,
                    AvatarVersion = c.AvatarVersion
                }).ToList();
        }

        public async Task<List<ChildDto>> GetAllChildrenAsync()
        {
            var children = await _childRepository.GetAllAsync();
            return children.Select(c => new ChildDto
                {
                    ChildId = c.ChildId,
                    FullName = c.FullName,
                    SchoolId = c.SchoolId,
                    SchoolName = c.School?.SchoolName ?? string.Empty,
                    CenterId = c.CenterId,
                    CenterName = c.Center?.Name,
                    Grade = c.Grade,
                    DateOfBirth = c.DateOfBirth,
                    Status = c.Status,
                    AvatarUrl = c.AvatarUrl,
                    AvatarVersion = c.AvatarVersion
                }).ToList();
        }

        public async Task LinkCenterAsync(Guid childId, LinkCenterRequest request)
        {
            var child = await _childRepository.GetByIdAsync(childId);
            if (child == null || child.Status == "deleted")
                throw new Exception("Child not found or deleted");

            var center = await _centerRepository.GetByIdAsync(request.CenterId);
            if (center == null)
                throw new Exception("Center not found");

            // Check if child already has a center
            if (child.CenterId.HasValue && child.CenterId != request.CenterId)
            {
                // Optional: Log or notify about center change
                // throw new Exception("Child is already linked to another center");
            }

            child.CenterId = request.CenterId;
            await _childRepository.UpdateAsync(child);
        }


        public async Task<string> UpdateChildProfilePictureAsync(Guid childId, UpdateProfilePictureCommand command, Guid currentUserId, string currentUserRole)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var child = await _childRepository.GetByIdAsync(childId);
            if (child == null || child.Status == "deleted")
                throw new Exception("Child not found or deleted");

            // Validate that the user is the parent of the child or is an admin
            if (string.IsNullOrEmpty(currentUserRole))
                throw new Exception("Unauthorized access");

            if (currentUserRole != "admin" && child.ParentId != currentUserId)
                throw new Exception("Unauthorized access: You can only update your own child's profile picture");

            // Validate file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = System.IO.Path.GetExtension(command.File.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Invalid file type. Only JPG, PNG and WebP are allowed.");

            if (command.File.Length > 2 * 1024 * 1024) // 2MB
                throw new ArgumentException("File size exceeds 2MB limit.");

            // Upload to Cloudinary
            string avatarUrl = await _cloudinaryService.UploadAvatarAsync(command.File, child.ChildId);

            // Update child entity
            child.AvatarUrl = avatarUrl;
            // Increment version to bust cache if needed, or just track updates
            child.AvatarVersion = (byte)((child.AvatarVersion ?? 0) + 1);

            await _childRepository.UpdateAsync(child);

            return avatarUrl;
        }

        public async Task<List<ContractDto>> GetChildContractsAsync(Guid childId)
        {
            var contracts = await _childRepository.GetContractsByChildIdAsync(childId);
            return contracts.Select(c => new ContractDto
            {
                ContractId = c.ContractId,
                ChildId = c.ChildId,
                ChildName = c.Child.FullName,
                PackageId = c.PackageId,
                PackageName = c.Package.PackageName,
                MainTutorId = c.MainTutorId,
                MainTutorName = c.MainTutor.FullName,
                CenterId = c.CenterId,
                CenterName = c.Center?.Name,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsOnline = c.IsOnline,
                Status = c.Status
            }).ToList();
        }
    }
}
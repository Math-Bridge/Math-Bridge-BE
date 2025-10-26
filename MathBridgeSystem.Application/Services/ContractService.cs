using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IChildRepository _childRepository;
        private readonly IPackageRepository _packageRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly ITutorCenterRepository _tutorCenterRepository;
        private readonly ILocationService _locationService;

        public ContractService(
            IContractRepository contractRepository,
            IChildRepository childRepository,
            IPackageRepository packageRepository,
            IUserRepository userRepository,
            ICenterRepository centerRepository,
            ITutorCenterRepository tutorCenterRepository,
            ILocationService locationService)
        {
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
            _childRepository = childRepository ?? throw new ArgumentNullException(nameof(childRepository));
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _centerRepository = centerRepository ?? throw new ArgumentNullException(nameof(centerRepository));
            _tutorCenterRepository = tutorCenterRepository ?? throw new ArgumentNullException(nameof(tutorCenterRepository));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        }

        private string FormatDaysOfWeek(byte? daysOfWeek)
        {
            if (!daysOfWeek.HasValue) return string.Empty;

            var days = new List<string>();
            var value = daysOfWeek.Value;

            if ((value & 1) != 0) days.Add("Sun");
            if ((value & 2) != 0) days.Add("Mon");
            if ((value & 4) != 0) days.Add("Tue");
            if ((value & 8) != 0) days.Add("Wed");
            if ((value & 16) != 0) days.Add("Thu");
            if ((value & 32) != 0) days.Add("Fri");
            if ((value & 64) != 0) days.Add("Sat");

            return string.Join(", ", days);
        }

        public async Task<Guid> CreateContractAsync(CreateContractRequest request)
        {
            var child = await _childRepository.GetByIdAsync(request.ChildId);
            if (child == null || child.Status == "deleted")
                throw new Exception("Child not found or deleted");

            // VALIDATION QUAN TRỌNG: Kiểm tra CenterId
            if (request.CenterId.HasValue)
            {
                // Nếu contract có CenterId, phải khớp với Child's CenterId hoặc Child chưa có Center
                if (child.CenterId.HasValue && child.CenterId != request.CenterId)
                {
                    throw new Exception("Contract center does not match child's assigned center");
                }

                // Nếu child chưa có center, tự động assign
                if (!child.CenterId.HasValue)
                {
                    var center = await _centerRepository.GetByIdAsync(request.CenterId.Value);
                    if (center == null)
                        throw new Exception("Center not found");

                    child.CenterId = request.CenterId;
                    await _childRepository.UpdateAsync(child);
                }
            }
            else
            {
                // Nếu contract không có CenterId, sử dụng CenterId của child (nếu có)
                if (child.CenterId.HasValue)
                {
                    request.CenterId = child.CenterId;
                }
                else
                {
                    // Cả contract và child đều không có center - có thể là online hoặc cần assign center
                    if (!request.IsOnline)
                    {
                        throw new Exception("Offline contract requires a center assignment");
                    }
                }
            }

            var parent = await _userRepository.GetByIdAsync(request.ParentId);
            if (parent == null || parent.RoleId != 3) // Assume 3 is 'parent'
                throw new Exception("Invalid parent");

            var package = await _packageRepository.GetByIdAsync(request.PackageId);
            if (package == null)
                throw new Exception("Package not found");

            var mainTutor = await _userRepository.GetByIdAsync(request.MainTutorId);
            if (mainTutor == null || mainTutor.RoleId != 2) // Assume 2 is 'tutor'
                throw new Exception("Invalid main tutor");

            // Validate substitute tutors
            if (request.SubstituteTutor1Id.HasValue)
            {
                var sub1 = await _userRepository.GetByIdAsync(request.SubstituteTutor1Id.Value);
                if (sub1 == null || sub1.RoleId != 2)
                    throw new Exception("Invalid substitute tutor 1");
            }

            if (request.SubstituteTutor2Id.HasValue)
            {
                var sub2 = await _userRepository.GetByIdAsync(request.SubstituteTutor2Id.Value);
                if (sub2 == null || sub2.RoleId != 2)
                    throw new Exception("Invalid substitute tutor 2");
            }

            // Validate tutor assignment to center (nếu có center)
            if (request.CenterId.HasValue)
            {
                var tutorCenterExists = await _tutorCenterRepository.TutorIsAssignedToCenterAsync(
                    request.MainTutorId, request.CenterId.Value);
                if (!tutorCenterExists)
                    throw new Exception($"Main tutor is not assigned to center {request.CenterId}");

                // Check substitute tutors
                if (request.SubstituteTutor1Id.HasValue)
                {
                    var sub1CenterExists = await _tutorCenterRepository.TutorIsAssignedToCenterAsync(
                        request.SubstituteTutor1Id.Value, request.CenterId.Value);
                    if (!sub1CenterExists)
                        throw new Exception("Substitute tutor 1 is not assigned to the specified center");
                }

                if (request.SubstituteTutor2Id.HasValue)
                {
                    var sub2CenterExists = await _tutorCenterRepository.TutorIsAssignedToCenterAsync(
                        request.SubstituteTutor2Id.Value, request.CenterId.Value);
                    if (!sub2CenterExists)
                        throw new Exception("Substitute tutor 2 is not assigned to the specified center");
                }
            }

            

            if (request.VideoCallPlatform != null && !new[] { "zoom", "google_meet" }.Contains(request.VideoCallPlatform))
                throw new Exception("Invalid video call platform");

            if (!request.StartTime.HasValue || !request.EndTime.HasValue)
                throw new Exception("Start and end times are required");

            if (request.EndTime.Value <= request.StartTime.Value)
                throw new Exception("End time must be after start time");

            if (request.StartTime.Value.Hour < 16 || request.EndTime.Value.Hour > 22)
                throw new Exception("Time must be between 16:00 and 22:00");

            if (!request.DaysOfWeeks.HasValue || request.DaysOfWeeks.Value < 1 || request.DaysOfWeeks.Value > 127)
                throw new Exception("DaysOfWeeks must be between 1 and 127");

            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                ParentId = request.ParentId,
                ChildId = request.ChildId,
                CenterId = request.CenterId,
                PackageId = request.PackageId,
                MainTutorId = request.MainTutorId,
                SubstituteTutor1Id = request.SubstituteTutor1Id,
                SubstituteTutor2Id = request.SubstituteTutor2Id,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DaysOfWeeks = request.DaysOfWeeks,
                IsOnline = request.IsOnline,
                OfflineAddress = request.OfflineAddress,
                OfflineLatitude = request.OfflineLatitude,
                OfflineLongitude = request.OfflineLongitude,
                VideoCallPlatform = request.VideoCallPlatform,
                MaxDistanceKm = request.MaxDistanceKm,
                RescheduleCount = 0,
                Status = "pending",
                CreatedDate = DateTime.UtcNow
            };

            await _contractRepository.AddAsync(contract);
            return contract.ContractId;
        }

        public async Task<List<ContractDto>> GetContractsByParentAsync(Guid parentId)
        {
            var contracts = await _contractRepository.GetByParentIdAsync(parentId);
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
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                DaysOfWeeks = c.DaysOfWeeks,
                DaysOfWeeksDisplay = FormatDaysOfWeek(c.DaysOfWeeks),
                IsOnline = c.IsOnline,
                Status = c.Status
            }).ToList();
        }
    }
}
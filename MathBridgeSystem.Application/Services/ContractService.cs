using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.Contract;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IPackageRepository _packageRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IChildRepository _childRepository;

        public ContractService(
            IContractRepository contractRepository,
            IPackageRepository packageRepository,
            ISessionRepository sessionRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            IChildRepository childRepository)
        {
            _contractRepository = contractRepository;
            _packageRepository = packageRepository;
            _sessionRepository = sessionRepository;
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _childRepository = childRepository ?? throw new ArgumentNullException(nameof(childRepository));
        }

        public async Task<Guid> CreateContractAsync(CreateContractRequest request)
        {
            // Validate status
            if (string.IsNullOrWhiteSpace(request.Status))
                throw new ArgumentException("Status is required.");

            var validStatuses = new[] { "pending", "active", "completed", "cancelled" };
            if (!validStatuses.Contains(request.Status.ToLowerInvariant()))
                throw new ArgumentException("Invalid status.");

            var package = await _packageRepository.GetByIdAsync(request.PackageId)
                          ?? throw new Exception("Package not found");

            // Validate twin children
            if (request.SecondChildId.HasValue)
            {
                var firstChild = await _childRepository.GetByIdAsync(request.ChildId)
                                 ?? throw new Exception("First child not found");
                var secondChild = await _childRepository.GetByIdAsync(request.SecondChildId.Value)
                                  ?? throw new Exception("Second child not found");

                if (firstChild.ParentId != request.ParentId || secondChild.ParentId != request.ParentId)
                    throw new ArgumentException("Both children must belong to the same parent");

                if (firstChild.Grade != secondChild.Grade)
                    throw new ArgumentException("Both children must be in the same grade for twin contract");

                if (firstChild.SchoolId != secondChild.SchoolId)
                    throw new ArgumentException("Both children must attend the same school for twin contract");
            }

            // VALIDATE FLEXIBLE SCHEDULES
            if (request.Schedules == null || request.Schedules.Count == 0)
                throw new ArgumentException("At least one schedule entry is required.");

            if (request.Schedules.Count > 7)
                throw new ArgumentException("Cannot select more than 7 days per week.");

            if (request.Schedules.GroupBy(s => s.DayOfWeek).Any(g => g.Count() > 1))
                throw new ArgumentException("Duplicate day of week is not allowed.");

            foreach (var s in request.Schedules)
            {
                if (s.StartTime >= s.EndTime)
                    throw new ArgumentException($"Start time must be earlier than end time for {s.DayOfWeek}");
            }

            var maxDistanceKm = request.MaxDistanceKm ?? 15;

            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                ParentId = request.ParentId,
                ChildId = request.ChildId,
                SecondChildId = request.SecondChildId,
                CenterId = request.CenterId,
                PackageId = request.PackageId,
                MainTutorId = request.MainTutorId,
                SubstituteTutor1Id = request.SubstituteTutor1Id,
                SubstituteTutor2Id = request.SubstituteTutor2Id,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsOnline = request.IsOnline,
                OfflineAddress = request.OfflineAddress,
                OfflineLatitude = request.OfflineLatitude,
                OfflineLongitude = request.OfflineLongitude,
                VideoCallPlatform = request.VideoCallPlatform,
                MaxDistanceKm = maxDistanceKm,
                RescheduleCount = package.MaxReschedule,
                Status = request.Status.ToLowerInvariant(),
                CreatedDate = DateTime.UtcNow
            };

            // Add flexible schedules
            foreach (var s in request.Schedules)
            {
                contract.ContractSchedules.Add(new ContractSchedule
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                });
            }

            if (!contract.IsOnline && request.CenterId == null)
                throw new ArgumentException("CenterId is required for offline contracts.");
            // Check main child
            var hasOverlapForMainChild = await _contractRepository.HasOverlappingContractForChildAsync(
                childId: request.ChildId,
                startDate: request.StartDate,
                endDate: request.EndDate,
                newSchedules: request.Schedules.Select(s => new ContractSchedule
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList(),
                excludeContractId: null
            );

            if (hasOverlapForMainChild)
            {
                throw new InvalidOperationException(
                            "This child already has a pending contract or is currently studying and the schedule conflicts." +
                            "Cannot create a new contract for this time.");
            }

            // check sub child (if have)
            if (request.SecondChildId.HasValue)
            {
                var hasOverlapForSecondChild = await _contractRepository.HasOverlappingContractForChildAsync(
                    childId: request.SecondChildId.Value,
                    startDate: request.StartDate,
                    endDate: request.EndDate,
                    newSchedules: request.Schedules.Select(s => new ContractSchedule
                    {
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    }).ToList(),
                    excludeContractId: null
                );

                if (hasOverlapForSecondChild)
                {
                    throw new InvalidOperationException(
                            "This child already has a pending contract or is currently studying and the schedule conflicts." +
                            "Cannot create a new contract for this time.");
                }
            }
            await _contractRepository.AddAsync(contract);

            // Generate sessions only if main tutor is assigned and contract is not cancelled
            if (contract.MainTutorId.HasValue && contract.Status != "cancelled")
            {
                var sessions = GenerateSessions(contract, package.SessionCount);
                if (sessions.Count < package.SessionCount)
                    throw new InvalidOperationException($"Not enough valid days to generate {package.SessionCount} sessions.");

                await _sessionRepository.AddRangeAsync(sessions);
            }

            return contract.ContractId;
        }

        // FIXED: This method was missing!
        public async Task<bool> AssignTutorsAsync(Guid contractId, AssignTutorToContractRequest request, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdWithPackageAsync(contractId)
                           ?? throw new KeyNotFoundException("Contract not found.");

            if (contract.Status == "cancelled")
                throw new InvalidOperationException("Cannot assign tutors to a cancelled contract.");

            if (request.MainTutorId == Guid.Empty)
                throw new ArgumentException("MainTutorId is required.");

            // Update tutors
            contract.MainTutorId = request.MainTutorId;
            contract.SubstituteTutor1Id = request.SubstituteTutor1Id;
            contract.SubstituteTutor2Id = request.SubstituteTutor2Id;
            contract.UpdatedDate = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(contract);

            // Generate sessions using the new flexible schedule
            var sessions = GenerateSessions(contract, contract.Package.SessionCount);
            await _sessionRepository.AddRangeAsync(sessions);

            return true;
        }

        private List<Session> GenerateSessions(Contract contract, int totalSessionsNeeded)
        {
            if (!contract.MainTutorId.HasValue)
                throw new InvalidOperationException("Main tutor must be assigned to generate sessions.");

            var sessions = new List<Session>();

            // BƯỚC 1: Tìm ngày bắt đầu hợp lệ đầu tiên (phải >= StartDate và chưa qua)
            var today = DateOnly.FromDateTime(DateTime.Today);
            var currentDate = contract.StartDate;

            // Nếu StartDate là quá khứ hoặc hôm nay → tìm ngày hợp lệ tiếp theo
            if (currentDate < today)
            {
                currentDate = today;
            }

            // BƯỚC 2: Từ ngày currentDate, tìm buổi học hợp lệ đầu tiên trong lịch
            // → Tức là: nếu hôm nay là Thứ 3, mà StartDate là Thứ 3 → bỏ qua hôm nay → tìm ngày tiếp theo trong lịch
            while (currentDate <= contract.EndDate && sessions.Count < totalSessionsNeeded)
            {
                var schedule = contract.ContractSchedules.FirstOrDefault(s => s.DayOfWeek == currentDate.DayOfWeek);

                if (schedule != null)
                {
                    var sessionDateTime = currentDate.ToDateTime(schedule.StartTime);

                    // Chỉ tạo buổi học nếu:
                    // Ngày đó là tương lai (ngày mai trở đi) hoặc
                    // Ngày đó là hôm nay nhưng giờ chưa tới (sessionStart > Now)
                    if (sessionDateTime > DateTime.Now ||
                        (currentDate > today)) 
                    {
                        sessions.Add(new Session
                        {
                            BookingId = Guid.NewGuid(),
                            ContractId = contract.ContractId,
                            TutorId = contract.MainTutorId.Value,
                            SessionDate = currentDate,
                            StartTime = sessionDateTime,
                            EndTime = currentDate.ToDateTime(schedule.EndTime),
                            IsOnline = contract.IsOnline,
                            VideoCallPlatform = contract.VideoCallPlatform,
                            OfflineAddress = contract.OfflineAddress,
                            OfflineLatitude = contract.OfflineLatitude,
                            OfflineLongitude = contract.OfflineLongitude,
                            Status = "scheduled",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            if (sessions.Count < totalSessionsNeeded)
            {
                throw new InvalidOperationException(
                    $"Only {sessions.Count} session(s) can be scheduled from the next available date to {contract.EndDate:dd/MM/yyyy}. " +
                    $"Need {totalSessionsNeeded}. Please extend the contract duration or adjust the schedule.");
            }

            return sessions;
        }

        public async Task<bool> UpdateContractStatusAsync(Guid contractId, UpdateContractStatusRequest request, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                           ?? throw new KeyNotFoundException("Contract not found.");

            var validStatuses = new[] { "active", "completed", "cancelled" };
            if (!validStatuses.Contains(request.Status.ToLowerInvariant()))
                throw new ArgumentException("Invalid status. Allowed: active, completed, cancelled");

            if (contract.Status == "cancelled" && request.Status.ToLowerInvariant() != "cancelled")
                throw new InvalidOperationException("Cannot reactivate a cancelled contract.");

            var oldStatus = contract.Status;
            contract.Status = request.Status.ToLowerInvariant();
            contract.UpdatedDate = DateTime.UtcNow;
            await _contractRepository.UpdateAsync(contract);

            if (request.Status.ToLowerInvariant() == "active" && oldStatus != "active")
            {
                var fullContract = await _contractRepository.GetByIdAsync(contractId);
                if (fullContract?.Parent == null || fullContract.Child == null ||
                    fullContract.Package == null || fullContract.MainTutor == null)
                    throw new InvalidOperationException("Missing data to send confirmation email.");

                var pdfBytes = ContractPdfGenerator.GenerateContractPdf(
                    fullContract, fullContract.Child, fullContract.Parent,
                    fullContract.Package, fullContract.MainTutor, fullContract.Center);

                await _emailService.SendContractConfirmationAsync(
                    email: fullContract.Parent.Email,
                    parentName: fullContract.Parent.FullName,
                    contractId: contract.ContractId,
                    pdfBytes: pdfBytes,
                    pdfFileName: $"MathBridge_Contract_{contract.ContractId}.pdf");
            }

            if (request.Status.ToLowerInvariant() == "cancelled")
            {
                var sessions = await _sessionRepository.GetByContractIdAsync(contractId);
                foreach (var s in sessions.Where(x => x.Status is "scheduled" or "rescheduled"))
                {
                    s.Status = "cancelled";
                    s.UpdatedAt = DateTime.UtcNow;
                    await _sessionRepository.UpdateAsync(s);
                }
            }

            return true;
        }

        public async Task<List<ContractDto>> GetContractsByParentAsync(Guid parentId)
            => (await _contractRepository.GetByParentIdAsync(parentId)).Select(MapToDto).ToList();

        public async Task<ContractDto> GetContractByIdAsync(Guid contractId)
            => MapToDto(await _contractRepository.GetByIdAsync(contractId)
                        ?? throw new KeyNotFoundException("Contract not found."));

        public async Task<List<ContractDto>> GetAllContractsAsync()
            => (await _contractRepository.GetAllWithDetailsAsync()).Select(MapToDto).ToList();

        public async Task<List<ContractDto>> GetContractsByParentPhoneAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty.");

            return (await _contractRepository.GetByParentPhoneNumberAsync(phoneNumber.Trim()))
                .Select(MapToDto).ToList();
        }

        private ContractDto MapToDto(Contract c) => new()
        {
            ContractId = c.ContractId,
            ChildId = c.ChildId,
            ChildName = c.Child?.FullName ?? "Unknown Child",
            SecondChildId = c.SecondChildId,
            SecondChildName = c.SecondChild?.FullName,
            PackageId = c.PackageId,
            PackageName = c.Package?.PackageName ?? "Unknown Package",
            Price = c.Package?.Price ?? 0,
            MainTutorId = c.MainTutorId,
            MainTutorName = c.MainTutor?.FullName ?? "Not Assigned",
            substitute_tutor1_id = c.SubstituteTutor1Id,
            substitute_tutor1_name = c.SubstituteTutor1?.FullName ?? "Not Assigned",
            substitute_tutor2_id = c.SubstituteTutor2Id,
            substitute_tutor2_name = c.SubstituteTutor2?.FullName ?? "Not Assigned",
            CenterId = c.CenterId,
            CenterName = c.Center?.Name ?? "No Center",
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            Schedules = c.ContractSchedules.Select(s => new ContractScheduleDto
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList(),
            IsOnline = c.IsOnline,
            VideoCallPlatform = c.IsOnline ? c.VideoCallPlatform : null,
            OfflineAddress = !c.IsOnline ? c.OfflineAddress : null,
            OfflineLatitude = !c.IsOnline ? c.OfflineLatitude : null,
            OfflineLongitude = !c.IsOnline ? c.OfflineLongitude : null,
            MaxDistanceKm = !c.IsOnline ? c.MaxDistanceKm : null,
            RescheduleCount = c.RescheduleCount ?? 0,
            Status = c.Status,
            ParentId = c.ParentId
        };

        public async Task<bool> CompleteContractAsync(Guid contractId, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                           ?? throw new KeyNotFoundException("Contract not found.");

            if (contract.Status != "active")
                throw new InvalidOperationException("Only active contracts can be completed.");

            var sessions = await _sessionRepository.GetByContractIdAsync(contractId);
            if (sessions.Any(s => s.Status != "completed"))
                throw new InvalidOperationException("All sessions must be completed first.");

            contract.Status = "completed";
            contract.UpdatedDate = DateTime.UtcNow;
            await _contractRepository.UpdateAsync(contract);

            return true;
        }

        public async Task<List<AvailableTutorResponse>> GetAvailableTutorsAsync(Guid contractId, bool sortByRating = false, bool sortByDistance = false)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                           ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

            var tutors = await _contractRepository.GetAvailableTutorsForContractAsync(contractId);

            var result = tutors.Select(t => new AvailableTutorResponse
            {
                UserId = t.UserId,
                FullName = t.FullName,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                AverageRating = t.FinalFeedbacks?.Any() == true
                    ? (decimal)t.FinalFeedbacks.Average(f => f.OverallSatisfactionRating)
                    : 0m,
                FeedbackCount = t.FinalFeedbacks?.Count ?? 0,
                DistanceKm = CalculateDistance(contract, t)
            }).ToList();

            if (sortByRating && sortByDistance)
                result = result.OrderByDescending(t => t.AverageRating)
                               .ThenBy(t => t.DistanceKm ?? decimal.MaxValue)
                               .ToList();
            else if (sortByRating)
                result = result.OrderByDescending(t => t.AverageRating)
                               .ThenByDescending(t => t.FeedbackCount)
                               .ToList();
            else if (sortByDistance)
                result = result.OrderBy(t => t.DistanceKm ?? decimal.MaxValue).ToList();

            return result;
        }

        private decimal? CalculateDistance(Contract contract, User tutor)
        {
            if (contract.IsOnline) return null;
            if (!contract.OfflineLatitude.HasValue || !contract.OfflineLongitude.HasValue ||
                !tutor.Latitude.HasValue || !tutor.Longitude.HasValue)
                return null;

            const double earthRadiusKm = 6371.0;
            var lat1 = (double)contract.OfflineLatitude.Value * Math.PI / 180.0;
            var lon1 = (double)contract.OfflineLongitude.Value * Math.PI / 180.0;
            var lat2 = tutor.Latitude.Value * Math.PI / 180.0;
            var lon2 = tutor.Longitude.Value * Math.PI / 180.0;

            var dLat = lat2 - lat1;
            var dLon = lon2 - lon1;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = earthRadiusKm * c;

            return (decimal)Math.Round(distance, 2);
        }
        public async Task<List<AvailableTutorResponse>> CheckTutorsAvailabilityBeforeCreateAsync(CheckTutorAvailabilityRequest request)
        {
            // 1. Validate lịch học
            if (request.Schedules == null || request.Schedules.Count == 0)
                throw new ArgumentException("Please select at least one schedule.");
            if (request.Schedules.Count > 7)
                throw new ArgumentException("Cannot select more than 7 days per week.");
            if (request.Schedules.GroupBy(s => s.DayOfWeek).Any(g => g.Count() > 1))
                throw new ArgumentException("Cannot have duplicate days.");

            foreach (var s in request.Schedules)
            {
                if (s.StartTime >= s.EndTime)
                    throw new ArgumentException($"Start time must be before end time for {s.DayOfWeek}");
            }

            // 2. Get all Childs Infor
            var firstChild = await _childRepository.GetByIdAsync(request.ChildId)
                             ?? throw new ArgumentException("First child not found.");

            Guid? firstCenterId = firstChild.CenterId;

            Guid? secondCenterId = null;
            if (request.SecondChildId.HasValue)
            {
                var secondChild = await _childRepository.GetByIdAsync(request.SecondChildId.Value)
                                  ?? throw new ArgumentException("Second child not found.");

                secondCenterId = secondChild.CenterId;

                // REQUIRED: Both children must attend the same center if studying offline
                if (!request.IsOnline && firstCenterId != secondCenterId)
                    throw new ArgumentException("Both children must be in the same center for offline twin contracts.");
            }

            // 3. IDENTIFY THE CENTER USED FOR TUTOR FILTERING (OFFLINE)
            var requiredCenterId = !request.IsOnline ? firstCenterId : null;

            if (!request.IsOnline && !requiredCenterId.HasValue)
                throw new ArgumentException("Offline classes require child(ren) to be assigned to a center.");

            // 4. Create a temporary contract
            var tempContract = new Contract
            {
                ChildId = request.ChildId,
                SecondChildId = request.SecondChildId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsOnline = request.IsOnline,
                OfflineLatitude = request.OfflineLatitude,
                OfflineLongitude = request.OfflineLongitude,
                MaxDistanceKm = request.MaxDistanceKm ?? 15,
                ContractSchedules = request.Schedules.Select(s => new ContractSchedule
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList()
            };

            // 5. Get all available tutors (no scheduling conflicts)
            var allAvailableTutors = await _contractRepository.GetAvailableTutorsForContractAsync(tempContract);

            // 6. FILTER BY CENTER + DISTANCE (OFFLINE ONLY)
            var finalTutors = request.IsOnline
                ? allAvailableTutors
                : allAvailableTutors.Where(t =>
                {
                    // Must be centered around child 1 (and child 2 if applicable)
                    bool inCorrectCenter = t.TutorCenters.Any(tc => tc.CenterId == requiredCenterId.Value);

                    // Must be within MaxDistanceKm
                    var distance = CalculateDistance(tempContract, t);
                    bool withinDistance = distance <= tempContract.MaxDistanceKm;

                    return inCorrectCenter && withinDistance;
                }).ToList();

            // 7. Return the result
            return finalTutors.Select(t => new AvailableTutorResponse
            {
                UserId = t.UserId,
                FullName = t.FullName,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                AverageRating = t.FinalFeedbacks?.Any() == true
                    ? (decimal)t.FinalFeedbacks.Average(f => f.OverallSatisfactionRating)
                    : 0m,
                FeedbackCount = t.FinalFeedbacks?.Count ?? 0,
                DistanceKm = !request.IsOnline ? CalculateDistance(tempContract, t) : null
            })
            .OrderBy(t => t.DistanceKm ?? decimal.MaxValue)
            .ThenByDescending(t => t.AverageRating)
            .ToList();
        }
    }
}
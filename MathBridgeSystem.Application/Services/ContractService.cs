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

        public ContractService(
            IContractRepository contractRepository,
            IPackageRepository packageRepository,
            ISessionRepository sessionRepository,
            IEmailService emailService,
            IUserRepository userRepository)
        {
            _contractRepository = contractRepository;
            _packageRepository = packageRepository;
            _sessionRepository = sessionRepository;
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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

<<<<<<< Updated upstream
            // VALIDATE & ÉP KIỂU DaysOfWeeks: int? → byte?
            byte? daysOfWeeksByte = null;
            if (request.DaysOfWeeks.HasValue)
=======
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
>>>>>>> Stashed changes
            {
                if (s.StartTime >= s.EndTime)
                    throw new ArgumentException($"Start time must be earlier than end time for {s.DayOfWeek}");
            }

            var maxDistanceKm = request.MaxDistanceKm ?? 15;

<<<<<<< Updated upstream
            // Check for overlapping contracts for the child
            var hasOverlap = await _contractRepository.HasOverlappingContractForChildAsync(
                childId: request.ChildId,
                startDate: request.StartDate,
                endDate: request.EndDate,
                startTime: request.StartTime,
                endTime: request.EndTime,
                daysOfWeeks: daysOfWeeksByte,
                excludeContractId: null
            );

            if (hasOverlap)
            {
                throw new InvalidOperationException(
                    "Cannot create contract: This child already has an overlapping contract with the same schedule. " +
                    "Please check the existing contracts or adjust the schedule (date range, days of week, or time slots).");
            }

=======
>>>>>>> Stashed changes
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
                contract.Schedules.Add(new ContractSchedule
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                });
            }

            if (!contract.IsOnline && request.CenterId == null)
                throw new ArgumentException("CenterId is required for offline contracts.");

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

<<<<<<< Updated upstream
        public async Task<bool> UpdateContractStatusAsync(Guid contractId, UpdateContractStatusRequest request, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null) throw new KeyNotFoundException("Contract not found.");

            var validNewStatuses = new[] { "active", "completed", "cancelled" };
            if (!validNewStatuses.Contains(request.Status.ToLower()))
                throw new ArgumentException("Invalid status. Use: active, completed, cancelled");

            if (contract.Status == "cancelled" && request.Status.ToLower() != "cancelled")
                throw new InvalidOperationException("Cannot reactivate a cancelled contract.");

            var oldStatus = contract.Status;
            contract.Status = request.Status.ToLower();
            contract.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            await _contractRepository.UpdateAsync(contract);

            // GỬI EMAIL KHI CHUYỂN SANG "active"
            if (request.Status.ToLower() == "active" && oldStatus != "active")
            {
                var fullContract = await _contractRepository.GetByIdAsync(contractId);
                if (fullContract == null) throw new KeyNotFoundException("Contract not found.");

                // Validate all required entities are present
                if (fullContract.Parent == null)
                    throw new InvalidOperationException("Cannot send email: Parent information is missing.");
                
                if (string.IsNullOrWhiteSpace(fullContract.Parent.Email))
                    throw new InvalidOperationException("Cannot send email: Parent email is missing.");

                if (fullContract.Child == null)
                    throw new InvalidOperationException("Cannot send email: Child information is missing.");

                if (fullContract.Package == null)
                    throw new InvalidOperationException("Cannot send email: Package information is missing.");

                if (fullContract.MainTutor == null)
                    throw new InvalidOperationException("Cannot send email: Main tutor information is missing.");

                var parent = fullContract.Parent;
                var child = fullContract.Child;
                var package = fullContract.Package;
                var mainTutor = fullContract.MainTutor;
                var center = fullContract.Center; // Center can be null

                var pdfBytes = ContractPdfGenerator.GenerateContractPdf(
                    fullContract, child, parent, package, mainTutor, center);

                await _emailService.SendContractConfirmationAsync(
                    email: parent.Email,
                    parentName: parent.FullName,
                    contractId: contract.ContractId,
                    pdfBytes: pdfBytes,
                    pdfFileName: $"MathBridge_Contract_{contract.ContractId}.pdf"
                );
            }

            if (request.Status.ToLower() == "cancelled")
            {
                var sessions = await _sessionRepository.GetByContractIdAsync(contractId);
                foreach (var s in sessions)
                {
                    if (s.Status == "scheduled" || s.Status == "rescheduled")
                    {
                        s.Status = "cancelled";
                        s.UpdatedAt = DateTime.UtcNow.ToLocalTime();
                        await _sessionRepository.UpdateAsync(s);
                    }
                }
            }

            return true;
        }

        public async Task<List<ContractDto>> GetContractsByParentAsync(Guid parentId)
        {
            var contracts = await _contractRepository.GetByParentIdAsync(parentId);
            return contracts.Select(c => new ContractDto
            {
                ContractId = c.ContractId,
                ChildId = c.ChildId,
                ChildName = c.Child?.FullName ?? "Unknown Child",
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
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                DaysOfWeeks = c.DaysOfWeeks,
                DaysOfWeeksDisplay = FormatDaysOfWeek(c.DaysOfWeeks),
                IsOnline = c.IsOnline,
                ParentId = c.ParentId,

                // ONLY ONLINE
                VideoCallPlatform = c.IsOnline ? c.VideoCallPlatform : null,

                // ONLY OFFLINE
                OfflineAddress = !c.IsOnline ? c.OfflineAddress : null,
                OfflineLatitude = !c.IsOnline ? c.OfflineLatitude : null,
                OfflineLongitude = !c.IsOnline ? c.OfflineLongitude : null,
                MaxDistanceKm = !c.IsOnline ? c.MaxDistanceKm : null,

                RescheduleCount = c.RescheduleCount ?? 0,
                Status = c.Status
            }).ToList();
        }

        public async Task<ContractDto> GetContractByIdAsync(Guid contractId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null)
                throw new KeyNotFoundException("Contract not found.");

            return new ContractDto
            {
                ContractId = contract.ContractId,
                ChildId = contract.ChildId,
                ChildName = contract.Child?.FullName ?? "Unknown Child",
                PackageId = contract.PackageId,
                PackageName = contract.Package?.PackageName ?? "Unknown Package",
                Price = contract.Package?.Price ?? 0,
                MainTutorId = contract.MainTutorId,
                MainTutorName = contract.MainTutor?.FullName ?? "Not Assigned",
                substitute_tutor1_id = contract.SubstituteTutor1Id,
                substitute_tutor1_name = contract.SubstituteTutor1?.FullName ?? "Not Assigned",
                substitute_tutor2_id = contract.SubstituteTutor2Id,
                substitute_tutor2_name = contract.SubstituteTutor2?.FullName ?? "Not Assigned",
                CenterId = contract.CenterId,
                CenterName = contract.Center?.Name ?? "No Center",
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                StartTime = contract.StartTime,
                EndTime = contract.EndTime,
                DaysOfWeeks = contract.DaysOfWeeks,
                DaysOfWeeksDisplay = FormatDaysOfWeek(contract.DaysOfWeeks),
                IsOnline = contract.IsOnline,
                ParentId = contract.ParentId,

                // ONLY ONLINE
                VideoCallPlatform = contract.IsOnline ? contract.VideoCallPlatform : null,

                // ONLY OFFLINE
                OfflineAddress = !contract.IsOnline ? contract.OfflineAddress : null,
                OfflineLatitude = !contract.IsOnline ? contract.OfflineLatitude : null,
                OfflineLongitude = !contract.IsOnline ? contract.OfflineLongitude : null,
                MaxDistanceKm = !contract.IsOnline ? contract.MaxDistanceKm : null,

                RescheduleCount = contract.RescheduleCount ?? 0,
                Status = contract.Status
            };
        }

=======
        // FIXED: This method was missing!
>>>>>>> Stashed changes
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
            var currentDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
            var endDate = contract.EndDate;

            while (currentDate <= endDate && sessions.Count < totalSessionsNeeded)
            {
                var schedule = contract.Schedules.FirstOrDefault(s =>
                    s.DayOfWeek == currentDate.DayOfWeek);

                if (schedule != null)
                {
                    var start = currentDate.ToDateTime(schedule.StartTime);
                    var end = currentDate.ToDateTime(schedule.EndTime);

                    sessions.Add(new Session
                    {
                        BookingId = Guid.NewGuid(),
                        ContractId = contract.ContractId,
                        TutorId = contract.MainTutorId.Value,
                        SessionDate = currentDate,
                        StartTime = start,
                        EndTime = end,
                        IsOnline = contract.IsOnline,
                        VideoCallPlatform = contract.VideoCallPlatform,
                        OfflineAddress = contract.OfflineAddress,
                        OfflineLatitude = contract.OfflineLatitude,
                        OfflineLongitude = contract.OfflineLongitude,
                        Status = "scheduled",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                currentDate = currentDate.AddDays(1);
            }

            if (sessions.Count < totalSessionsNeeded)
                throw new InvalidOperationException($"Only {sessions.Count} session(s) generated. Need {totalSessionsNeeded}.");

            return sessions;
        }
<<<<<<< Updated upstream
        private bool IsDayOfWeekSelected(DayOfWeek day, byte daysOfWeek)
=======

        public async Task<bool> UpdateContractStatusAsync(Guid contractId, UpdateContractStatusRequest request, Guid staffId)
>>>>>>> Stashed changes
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                           ?? throw new KeyNotFoundException("Contract not found.");

<<<<<<< Updated upstream
        private string FormatDaysOfWeek(byte? daysOfWeek)
        {
            if (!daysOfWeek.HasValue || daysOfWeek == 0)
                return "Do Not Have sessions";
=======
            var validStatuses = new[] { "active", "completed", "cancelled" };
            if (!validStatuses.Contains(request.Status.ToLowerInvariant()))
                throw new ArgumentException("Invalid status. Allowed: active, completed, cancelled");
>>>>>>> Stashed changes

            if (contract.Status == "cancelled" && request.Status.ToLowerInvariant() != "cancelled")
                throw new InvalidOperationException("Cannot reactivate a cancelled contract.");

            var oldStatus = contract.Status;
            contract.Status = request.Status.ToLowerInvariant();
            contract.UpdatedDate = DateTime.UtcNow;
            await _contractRepository.UpdateAsync(contract);

            if (request.Status.ToLowerInvariant() == "active" && oldStatus != "active")
            {
<<<<<<< Updated upstream
                ContractId = c.ContractId,
                ChildId = c.ChildId,
                ChildName = c.Child?.FullName ?? "Unknown Child",
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
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                DaysOfWeeks = c.DaysOfWeeks,
                DaysOfWeeksDisplay = FormatDaysOfWeek(c.DaysOfWeeks),
                IsOnline = c.IsOnline,
                ParentId = c.ParentId,
=======
                var fullContract = await _contractRepository.GetByIdAsync(contractId);
                if (fullContract?.Parent == null || fullContract.Child == null ||
                    fullContract.Package == null || fullContract.MainTutor == null)
                    throw new InvalidOperationException("Missing data to send confirmation email.");
>>>>>>> Stashed changes

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

<<<<<<< Updated upstream
            var normalizedPhone = phoneNumber.Trim();
            var contracts = await _contractRepository.GetByParentPhoneNumberAsync(normalizedPhone);

            return contracts.Select(c => new ContractDto
            {
                ContractId = c.ContractId,
                ChildId = c.ChildId,
                ChildName = c.Child?.FullName ?? "Unknown Child",
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
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                DaysOfWeeks = c.DaysOfWeeks,
                DaysOfWeeksDisplay = FormatDaysOfWeek(c.DaysOfWeeks),
                IsOnline = c.IsOnline,
                VideoCallPlatform = c.IsOnline ? c.VideoCallPlatform : null,
                OfflineAddress = !c.IsOnline ? c.OfflineAddress : null,
                OfflineLatitude = !c.IsOnline ? c.OfflineLatitude : null,
                OfflineLongitude = !c.IsOnline ? c.OfflineLongitude : null,
                MaxDistanceKm = !c.IsOnline ? c.MaxDistanceKm : null,
                RescheduleCount = c.RescheduleCount ?? 0,
                ParentId = c.ParentId,
                Status = c.Status
            }).ToList();
=======
            return (await _contractRepository.GetByParentPhoneNumberAsync(phoneNumber.Trim()))
                .Select(MapToDto).ToList();
>>>>>>> Stashed changes
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
            Schedules = c.Schedules.Select(s => new ContractScheduleDto
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
    }
}
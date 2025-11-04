using MathBridgeSystem.Application.DTOs;
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

        public ContractService(
            IContractRepository contractRepository,
            IPackageRepository packageRepository,
            ISessionRepository sessionRepository)
        {
            _contractRepository = contractRepository;
            _packageRepository = packageRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task<Guid> CreateContractAsync(CreateContractRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                throw new ArgumentException("Status is required.");

            var validStatuses = new[] { "pending", "active", "completed", "cancelled" };
            if (!validStatuses.Contains(request.Status.ToLower()))
                throw new ArgumentException("Invalid status.");

            var package = await _packageRepository.GetByIdAsync(request.PackageId);
            if (package == null) throw new Exception("Package not found");

            // VALIDATE & ÉP KIỂU DaysOfWeeks: int? → byte?
            byte? daysOfWeeksByte = null;
            if (request.DaysOfWeeks.HasValue)
            {
                if (request.DaysOfWeeks < 0 || request.DaysOfWeeks > 127)
                    throw new ArgumentOutOfRangeException("daysOfWeeks", "Bitmask must be between 0 and 127 (7 days).");

                if (request.DaysOfWeeks == 0)
                    throw new ArgumentException("At least one day of the week must be selected.");

                daysOfWeeksByte = (byte)request.DaysOfWeeks.Value;
            }

            var maxDistanceKm = request.MaxDistanceKm ?? 15;

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
                DaysOfWeeks = daysOfWeeksByte, 
                IsOnline = request.IsOnline,
                OfflineAddress = request.OfflineAddress,
                OfflineLatitude = request.OfflineLatitude,
                OfflineLongitude = request.OfflineLongitude,
                VideoCallPlatform = request.VideoCallPlatform,
                MaxDistanceKm = maxDistanceKm,
                RescheduleCount = package.MaxReschedule, 
                Status = request.Status.ToLower(),
                CreatedDate = DateTime.UtcNow
            };

            await _contractRepository.AddAsync(contract);

            // Tạo buổi học
            if (contract.MainTutorId.HasValue && contract.Status != "cancelled")
            {
                var sessions = GenerateSessions(contract, request, package.SessionCount);
                if (sessions.Count < package.SessionCount)
                    throw new InvalidOperationException($"Not enough days to create {package.SessionCount} lessons.");

                await _sessionRepository.AddRangeAsync(sessions);
            }

            return contract.ContractId;
        }

        public async Task<bool> UpdateContractStatusAsync(Guid contractId, UpdateContractStatusRequest request, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null) throw new KeyNotFoundException("Contract not found.");

            var validNewStatuses = new[] { "active", "completed", "cancelled" };
            if (!validNewStatuses.Contains(request.Status.ToLower()))
                throw new ArgumentException("Invalid status. Use: active, completed, cancelled");

            if (contract.Status == "cancelled" && request.Status.ToLower() != "cancelled")
                throw new InvalidOperationException("Cannot reactivate a cancelled contract.");

            contract.Status = request.Status.ToLower();
            contract.UpdatedDate = DateTime.UtcNow;
            await _contractRepository.UpdateAsync(contract);

            if (request.Status.ToLower() == "cancelled")
            {
                var sessions = await _sessionRepository.GetByContractIdAsync(contractId);
                foreach (var s in sessions)
                {
                    if (s.Status == "scheduled" || s.Status == "rescheduled")
                    {
                        s.Status = "cancelled";
                        s.UpdatedAt = DateTime.UtcNow;
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
                MainTutorId = c.MainTutorId,
                MainTutorName = c.MainTutor?.FullName ?? "Not Assigned",
                CenterId = c.CenterId,
                CenterName = c.Center?.Name ?? "No Center",
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                DaysOfWeeks = c.DaysOfWeeks,
                DaysOfWeeksDisplay = FormatDaysOfWeek(c.DaysOfWeeks),
                IsOnline = c.IsOnline,

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

        public async Task<bool> AssignTutorsAsync(Guid contractId, AssignTutorToContractRequest request, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdWithPackageAsync(contractId);
            if (contract == null) throw new KeyNotFoundException("Contract not found.");
            if (contract.MainTutorId.HasValue) throw new InvalidOperationException("Main tutor already assigned.");
            if (contract.Status == "cancelled") throw new InvalidOperationException("Cannot assign tutors to cancelled contract.");

            if (request.MainTutorId == Guid.Empty)
                throw new ArgumentException("MainTutorId is required.");

            contract.MainTutorId = request.MainTutorId;
            contract.SubstituteTutor1Id = request.SubstituteTutor1Id;
            contract.SubstituteTutor2Id = request.SubstituteTutor2Id;
            contract.UpdatedDate = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(contract);

            var createRequest = new CreateContractRequest
            {
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                StartTime = contract.StartTime,
                EndTime = contract.EndTime,
                DaysOfWeeks = contract.DaysOfWeeks,
                IsOnline = contract.IsOnline,
                VideoCallPlatform = contract.VideoCallPlatform,
                OfflineAddress = contract.OfflineAddress,
                OfflineLatitude = contract.OfflineLatitude,
                OfflineLongitude = contract.OfflineLongitude
            };

            var sessions = GenerateSessions(contract, createRequest, contract.Package.SessionCount);
            await _sessionRepository.AddRangeAsync(sessions);

            return true;
        }

        private List<Session> GenerateSessions(Contract contract, CreateContractRequest request, int totalSessionsNeeded)
        {
            if (!contract.MainTutorId.HasValue)
                throw new InvalidOperationException("MainTutorId is required to generate sessions.");

            var sessions = new List<Session>();
            var currentDate = request.StartDate;
            var endDate = request.EndDate;

            // LẤY TỪ contract.DaysOfWeeks (byte?)
            var daysOfWeeks = contract.DaysOfWeeks
                ?? throw new ArgumentException("DaysOfWeeks is required to generate sessions.");

            var startTime = request.StartTime
                ?? throw new ArgumentException("StartTime is required.");
            var endTime = request.EndTime
                ?? throw new ArgumentException("EndTime is required.");

            var sessionStartTime = new TimeOnly(startTime.Hour, startTime.Minute);
            var sessionEndTime = new TimeOnly(endTime.Hour, endTime.Minute);

            while (currentDate <= endDate && sessions.Count < totalSessionsNeeded)
            {
                // SỬA: DÙNG daysOfWeeks trực tiếp (đã đảm bảo != null)
                if (IsDayOfWeekSelected(currentDate.DayOfWeek, daysOfWeeks))
                {
                    var sessionStart = currentDate.ToDateTime(sessionStartTime);
                    var sessionEnd = currentDate.ToDateTime(sessionEndTime);

                    sessions.Add(new Session
                    {
                        BookingId = Guid.NewGuid(),
                        ContractId = contract.ContractId,
                        TutorId = contract.MainTutorId.Value,
                        SessionDate = currentDate,
                        StartTime = sessionStart,
                        EndTime = sessionEnd,
                        IsOnline = request.IsOnline,
                        VideoCallPlatform = request.VideoCallPlatform,
                        OfflineAddress = request.OfflineAddress,
                        OfflineLatitude = request.OfflineLatitude,
                        OfflineLongitude = request.OfflineLongitude,
                        Status = "scheduled",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                currentDate = currentDate.AddDays(1);
            }

            return sessions;
        }
        private bool IsDayOfWeekSelected(DayOfWeek day, byte daysOfWeek)
        {
            return (daysOfWeek & (1 << ((int)day))) != 0;
        }

        private string FormatDaysOfWeek(byte? daysOfWeek)
        {
            if (!daysOfWeek.HasValue || daysOfWeek == 0)
                return "Do Not Have sessions";

            var days = new List<string>();
            var value = daysOfWeek.Value; 

            if ((value & 1) != 0) days.Add("CN");
            if ((value & 2) != 0) days.Add("T2");
            if ((value & 4) != 0) days.Add("T3");
            if ((value & 8) != 0) days.Add("T4");
            if ((value & 16) != 0) days.Add("T5");
            if ((value & 32) != 0) days.Add("T6");
            if ((value & 64) != 0) days.Add("T7");

            return string.Join(", ", days);
        }
        public async Task<List<ContractDto>> GetAllContractsAsync()
        {
            var contracts = await _contractRepository.GetAllWithDetailsAsync();
            return contracts.Select(c => new ContractDto
            {
                ContractId = c.ContractId,
                ChildId = c.ChildId,
                ChildName = c.Child?.FullName ?? "Unknown Child",
                PackageId = c.PackageId,
                PackageName = c.Package?.PackageName ?? "Unknown Package",
                MainTutorId = c.MainTutorId,
                MainTutorName = c.MainTutor?.FullName ?? "Not Assigned",
                CenterId = c.CenterId,
                CenterName = c.Center?.Name ?? "No Center",
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                DaysOfWeeks = c.DaysOfWeeks,
                DaysOfWeeksDisplay = FormatDaysOfWeek(c.DaysOfWeeks),
                IsOnline = c.IsOnline,

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
    }
}
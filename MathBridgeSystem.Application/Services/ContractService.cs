// MathBridgeSystem.Application.Services/ContractService.cs
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
            var package = await _packageRepository.GetByIdAsync(request.PackageId);
            if (package == null) throw new Exception("Package not found");

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

            var sessions = GenerateSessions(contract, request, package.SessionCount);
            if (sessions.Count < package.SessionCount)
                throw new InvalidOperationException($"Không đủ ngày để tạo {package.SessionCount} buổi học.");

            await _sessionRepository.AddRangeAsync(sessions);

            return contract.ContractId;
        }

        private List<Session> GenerateSessions(Contract contract, CreateContractRequest request, int totalSessionsNeeded)
        {
            var sessions = new List<Session>();
            var currentDate = request.StartDate;
            var endDate = request.EndDate;
            var startTime = new TimeOnly(request.StartTime!.Value.Hour, request.StartTime.Value.Minute);
            var endTime = new TimeOnly(request.EndTime!.Value.Hour, request.EndTime.Value.Minute);

            while (currentDate <= endDate && sessions.Count < totalSessionsNeeded)
            {
                if (IsDayOfWeekSelected(currentDate.DayOfWeek, request.DaysOfWeeks!.Value))
                {
                    var sessionStart = currentDate.ToDateTime(startTime);
                    var sessionEnd = currentDate.ToDateTime(endTime);

                    var session = new Session
                    {
                        BookingId = Guid.NewGuid(),
                        ContractId = contract.ContractId,
                        TutorId = (Guid)contract.MainTutorId,
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
                    };

                    sessions.Add(session);
                }

                currentDate = currentDate.AddDays(1);
            }

            return sessions;
        }

        private bool IsDayOfWeekSelected(DayOfWeek day, byte daysOfWeek)
        {
            return (daysOfWeek & (1 << ((int)day))) != 0;
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
    }
}
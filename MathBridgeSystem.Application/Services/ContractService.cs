﻿using MathBridgeSystem.Application.DTOs;
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

        public ContractService(
            IContractRepository contractRepository,
            IPackageRepository packageRepository,
            ISessionRepository sessionRepository,
            IEmailService emailService)
        {
            _contractRepository = contractRepository;
            _packageRepository = packageRepository;
            _sessionRepository = sessionRepository;
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService)); 
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
                CreatedDate = DateTime.UtcNow.ToLocalTime()
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

        public async Task<bool> AssignTutorsAsync(Guid contractId, AssignTutorToContractRequest request, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdWithPackageAsync(contractId);
            if (contract == null) throw new KeyNotFoundException("Contract not found.");
            if (contract.Status == "cancelled") throw new InvalidOperationException("Cannot assign tutors to cancelled contract.");

            if (request.MainTutorId == Guid.Empty)
                throw new ArgumentException("MainTutorId is required.");

            contract.MainTutorId = request.MainTutorId;
            contract.SubstituteTutor1Id = request.SubstituteTutor1Id;
            contract.SubstituteTutor2Id = request.SubstituteTutor2Id;
            contract.UpdatedDate = DateTime.UtcNow.ToLocalTime();

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
                        CreatedAt = DateTime.UtcNow.ToLocalTime()
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

        public async Task<List<ContractDto>> GetContractsByParentPhoneAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty.");

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
                Status = c.Status
            }).ToList();
        }
        public async Task<bool> CompleteContractAsync(Guid contractId, Guid staffId)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null)
                throw new KeyNotFoundException("Contract not found.");

            if (contract.Status != "active")
                throw new InvalidOperationException("Only active contracts can be completed.");

            var sessions = await _sessionRepository.GetByContractIdAsync(contractId);
            if (sessions.Any(s => s.Status != "completed"))
                throw new InvalidOperationException("All sessions must be completed before completing the contract.");

            contract.Status = "completed";
            contract.UpdatedDate = DateTime.UtcNow.ToLocalTime();

            await _contractRepository.UpdateAsync(contract);
            return true;
        }

        public async Task<List<AvailableTutorResponse>> GetAvailableTutorsAsync(Guid contractId)
        {
            try
            {
                // Get available tutors from repository
                var availableTutors = await _contractRepository.GetAvailableTutorsForContractAsync(contractId);

                // Map to response DTOs and sort by rating (descending), then by review count (descending)
                var result = availableTutors
                    .Select(tutor => new AvailableTutorResponse
                    {
                        UserId = tutor.UserId,
                        FullName = tutor.FullName,
                        Email = tutor.Email,
                        PhoneNumber = tutor.PhoneNumber,
                        AverageRating = tutor.FinalFeedbacks != null && tutor.FinalFeedbacks.Count > 0
                            ? (decimal)tutor.FinalFeedbacks.Average(f => f.OverallSatisfactionRating)
                            : 0m,
                        FeedbackCount = tutor.FinalFeedbacks != null ? tutor.FinalFeedbacks.Count : 0
                    })
                    .OrderByDescending(t => t.AverageRating)
                    .ThenByDescending(t => t.FeedbackCount)
                    .ToList();

                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving available tutors: {ex.Message}", ex);
            }
        }
    }
}


using MathBridgeSystem.Application.DTOs.Report;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathBridgeSystem.Application.Interfaces;

namespace MathBridgeSystem.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IUserRepository _userRepository;
        private readonly IContractRepository _contractRepository;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        // Role constants
        private const int TutorRoleId = 2;
        private const int ParentRoleId = 3;

        public ReportService(
            IReportRepository reportRepository, 
            IUserRepository userRepository, 
            IContractRepository contractRepository,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task<ReportResponseDto> CreateReportAsync(CreateReportDto dto, Guid userId, int roleId)
        {
            if (dto == null) 
                throw new ArgumentNullException(nameof(dto));

            // Validate role - only tutor (2) and parent (3) can create reports
            if (roleId != TutorRoleId && roleId != ParentRoleId)
                throw new UnauthorizedAccessException("Only tutors and parents can create reports.");

            // Force type based on role
            string reportType = roleId == TutorRoleId ? "tutor" : "parent";

            // Get the contract
            var contract = await _contractRepository.GetByIdAsync(dto.ContractId);
            if (contract == null)
                throw new KeyNotFoundException($"Contract with ID {dto.ContractId} not found.");

            Guid parentId;
            Guid tutorId;

            if (roleId == ParentRoleId)
            {
                // Parent creating report - validate parent owns the contract and tutor is assigned
                parentId = userId;

                if (contract.ParentId != parentId)
                    throw new UnauthorizedAccessException("You can only create reports for your own contracts.");

                if (!dto.TutorId.HasValue)
                    throw new ArgumentException("TutorId is required when creating a parent report.");

                tutorId = dto.TutorId.Value;

                // Validate tutor is assigned to this contract
                if (contract.MainTutorId != tutorId && 
                    contract.SubstituteTutor1Id != tutorId && 
                    contract.SubstituteTutor2Id != tutorId)
                {
                    throw new ArgumentException("The specified tutor is not assigned to this contract.");
                }

                // Spam protection for parents
                var lastReport = await _reportRepository.GetLatestReportByParentIdAsync(parentId);
                if (lastReport != null && 
                    DateTime.UtcNow.ToLocalTime().Subtract(lastReport.CreatedDate.ToDateTime(TimeOnly.MinValue)).TotalMinutes < 5)
                {
                    throw new InvalidOperationException("You can only create a report once every 5 minutes.");
                }
            }
            else // Tutor creating report
            {
                tutorId = userId;

                // Validate tutor is assigned to this contract
                if (contract.MainTutorId != tutorId && 
                    contract.SubstituteTutor1Id != tutorId && 
                    contract.SubstituteTutor2Id != tutorId)
                {
                    throw new UnauthorizedAccessException("You can only create reports for contracts you are assigned to.");
                }

                // ParentId comes from contract for tutor reports
                parentId = contract.ParentId;
            }

            var report = new Report
            {
                ReportId = Guid.NewGuid(),
                ParentId = parentId,
                TutorId = tutorId,
                Content = dto.Content,
                Url = dto.Url,
                Status = "pending",
                CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime()),
                Type = reportType,
                ContractId = dto.ContractId
            };

            await _reportRepository.AddAsync(report);

            // Send Notification and Email to the report creator
            var creator = await _userRepository.GetByIdAsync(userId);
            if (creator != null)
            {
                await _notificationService.CreateNotificationAsync(new MathBridgeSystem.Application.DTOs.Notification.CreateNotificationRequest
                {
                    UserId = userId,
                    ContractId = dto.ContractId,
                    Title = "Report Submitted",
                    Message = $"Your report (ID: {report.ReportId}) has been submitted successfully.",
                    NotificationType = "Report"
                });

                await _emailService.SendReportSubmittedAsync(creator.Email, creator.FullName, report.ReportId);
            }

            return await MapToDtoAsync(report);
        }

        public async Task<ReportResponseDto> UpdateReportAsync(Guid reportId, UpdateReportDto dto, Guid userId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var report = await _reportRepository.GetByIdAsync(reportId);
            if (report == null)
                throw new KeyNotFoundException($"Report with ID {reportId} not found.");

            // Validate ownership - only the creator can update their report
            bool isOwner = (report.Type == "parent" && report.ParentId == userId) ||
                          (report.Type == "tutor" && report.TutorId == userId);

            if (!isOwner)
                throw new UnauthorizedAccessException("You can only update your own reports.");

            // Only allow updates if status is still pending
            if (!string.Equals(report.Status, "pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot update a report that has already been processed.");

            // Update allowed fields (Type is NOT updated)
            if (!string.IsNullOrWhiteSpace(dto.Content))
                report.Content = dto.Content;

            if (dto.Url != null)
                report.Url = dto.Url;

            await _reportRepository.UpdateAsync(report);

            return await MapToDtoAsync(report);
        }

        public async Task DeleteReportAsync(Guid id)
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                throw new KeyNotFoundException($"Report with ID {id} not found.");

            await _reportRepository.DeleteAsync(report);
        }

        public async Task<ReportResponseDto> UpdateStatusAsync(Guid id, UpdateReportStatusDto dto)
        {
            if (dto == null) 
                throw new ArgumentNullException(nameof(dto));

            var validStatuses = new[] { "approved", "denied", "pending" };
            if (!validStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid status value. Valid values are: approved, denied, pending.");

            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                throw new KeyNotFoundException($"Report with ID {id} not found.");

            report.Status = dto.Status.ToLower();
            await _reportRepository.UpdateAsync(report);

            // Send Notification and Email to the report creator
            var notifyUserId = report.Type == "parent" ? report.ParentId : report.TutorId;
            var user = await _userRepository.GetByIdAsync(notifyUserId);
            if (user != null)
            {
                await _notificationService.CreateNotificationAsync(new MathBridgeSystem.Application.DTOs.Notification.CreateNotificationRequest
                {
                    UserId = notifyUserId,
                    ContractId = report.ContractId,
                    Title = $"Report {dto.Status}",
                    Message = $"Your report status has been updated to {dto.Status}. Reason: {dto.Reason ?? "N/A"}",
                    NotificationType = "Report"
                });

                await _emailService.SendReportStatusUpdateAsync(
                    user.Email, 
                    user.FullName, 
                    report.ReportId, 
                    dto.Status, 
                    dto.Reason ?? "No reason provided");
            }

            return await MapToDtoAsync(report);
        }

        public async Task<List<ReportResponseDto>> GetAllReportsAsync()
        {
            var reports = await _reportRepository.GetAllAsync();
            var dtos = new List<ReportResponseDto>();
            foreach (var report in reports)
            {
                dtos.Add(await MapToDtoAsync(report));
            }
            return dtos;
        }

        public async Task<ReportResponseDto> GetReportByIdAsync(Guid id)
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                throw new KeyNotFoundException($"Report with ID {id} not found.");

            return await MapToDtoAsync(report);
        }

        public async Task<List<ReportResponseDto>> GetReportsByParentIdAsync(Guid parentId)
        {
            var reports = await _reportRepository.GetByParentIdAsync(parentId);
            var dtos = new List<ReportResponseDto>();
            foreach (var report in reports)
            {
                dtos.Add(await MapToDtoAsync(report));
            }
            return dtos;
        }

        public async Task<List<ReportResponseDto>> GetReportsByTutorIdAsync(Guid tutorId)
        {
            var reports = await _reportRepository.GetByTutorIdAsync(tutorId);
            var dtos = new List<ReportResponseDto>();
            foreach (var report in reports)
            {
                dtos.Add(await MapToDtoAsync(report));
            }
            return dtos;
        }

        private async Task<ReportResponseDto> MapToDtoAsync(Report report)
        {
            var parent = report.Parent ?? await _userRepository.GetByIdAsync(report.ParentId);
            var tutor = report.Tutor ?? await _userRepository.GetByIdAsync(report.TutorId);

            return new ReportResponseDto
            {
                ReportId = report.ReportId,
                ParentId = report.ParentId,
                Parent = parent != null ? MapUserToResponse(parent) : null!,
                TutorId = report.TutorId,
                Tutor = tutor != null ? MapUserToResponse(tutor) : null!,
                Content = report.Content,
                Url = report.Url,
                Status = report.Status,
                CreatedDate = report.CreatedDate,
                Type = report.Type,
                ContractId = report.ContractId
            };
        }

        private UserResponse MapUserToResponse(User user)
        {
            return new UserResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                WalletBalance = user.WalletBalance,
                RoleId = user.RoleId,
                Status = user.Status,
                FormattedAddress = user.FormattedAddress,
                placeId = user.GooglePlaceId,
                avatarUrl = user.AvatarUrl,
                avatarVersion = user.AvatarVersion
            };
        }
    }
}
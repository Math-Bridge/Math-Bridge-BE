using MathBridgeSystem.Application.DTOs.Report;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IUserRepository _userRepository;
        private readonly IContractRepository _contractRepository;

        public ReportService(IReportRepository reportRepository, IUserRepository userRepository, IContractRepository contractRepository)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
        }

        public async Task<ReportResponseDto> CreateReportAsync(CreateReportDto dto, Guid ParentId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            // Spam Protection
            var lastReport = await _reportRepository.GetLatestReportByParentIdAsync(ParentId);
            if (lastReport != null && 
                DateTime.UtcNow.ToLocalTime().Subtract(lastReport.CreatedDate.ToDateTime(TimeOnly.MinValue)).TotalMinutes < 5)
            {
                throw new InvalidOperationException("You can only create a report once every 5 minutes.");
            }
            var contract = await _contractRepository.GetByIdAsync(dto.ContractId);
            if (contract == null || 
                contract.ParentId != ParentId || 
                (contract.MainTutorId != dto.TutorId && 
                 contract.SubstituteTutor1Id != dto.TutorId && 
                 contract.SubstituteTutor2Id != dto.TutorId))
            {
                throw new ArgumentException("The specified contract does not exist or does not match the parent and tutor IDs.");
            }

            var report = new Report
            {
                ReportId = Guid.NewGuid(),
                ParentId = ParentId,
                TutorId = dto.TutorId,
                Content = dto.Content,
                Url = dto.Url,
                Status = "Pending",
                CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow.ToLocalTime()),
                Type = dto.Type,
                ContractId = dto.ContractId
            };

            await _reportRepository.AddAsync(report);
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
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var validStatuses = new[] { "Approved", "Denied", "Pending" };
            if (!validStatuses.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid status value.");
            }

            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                throw new KeyNotFoundException($"Report with ID {id} not found.");

            report.Status = dto.Status;
            await _reportRepository.UpdateAsync(report);

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
            // Basic mapping - assuming simple UserResponse mapping logic or fetching users if navigation properties are null
            // Ideally, we use AutoMapper, but manual mapping for now to be safe.
            
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
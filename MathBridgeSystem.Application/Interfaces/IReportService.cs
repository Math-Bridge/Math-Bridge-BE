using MathBridgeSystem.Application.DTOs.Report;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services.Interfaces
{
    public interface IReportService
    {
        /// <summary>
        /// Creates a new report. Type is automatically set based on roleId:
        /// - RoleId 2 (tutor) ? Type = "tutor"
        /// - RoleId 3 (parent) ? Type = "parent"
        /// </summary>
        /// <param name="dto">Report creation data</param>
        /// <param name="userId">The ID of the user creating the report</param>
        /// <param name="roleId">The role ID of the user (2 = tutor, 3 = parent)</param>
        Task<ReportResponseDto> CreateReportAsync(CreateReportDto dto, Guid userId, int roleId);

        /// <summary>
        /// Updates an existing report. Type cannot be modified.
        /// </summary>
        /// <param name="reportId">The report ID</param>
        /// <param name="dto">Update data</param>
        /// <param name="userId">The ID of the user updating the report</param>
        Task<ReportResponseDto> UpdateReportAsync(Guid reportId, UpdateReportDto dto, Guid userId);

        Task DeleteReportAsync(Guid id);
        Task<ReportResponseDto> UpdateStatusAsync(Guid id, UpdateReportStatusDto dto);
        Task<List<ReportResponseDto>> GetAllReportsAsync();
        Task<ReportResponseDto> GetReportByIdAsync(Guid id);
        Task<List<ReportResponseDto>> GetReportsByParentIdAsync(Guid parentId);
        Task<List<ReportResponseDto>> GetReportsByTutorIdAsync(Guid tutorId);
    }
}
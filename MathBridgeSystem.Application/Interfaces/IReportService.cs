using MathBridgeSystem.Application.DTOs.Report;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services.Interfaces
{
    public interface IReportService
    {
        Task<ReportResponseDto> CreateReportAsync(CreateReportDto dto, Guid ParentId);
        Task DeleteReportAsync(Guid id);
        Task<ReportResponseDto> UpdateStatusAsync(Guid id, UpdateReportStatusDto dto);
        Task<List<ReportResponseDto>> GetAllReportsAsync();
        Task<ReportResponseDto> GetReportByIdAsync(Guid id);
        Task<List<ReportResponseDto>> GetReportsByParentIdAsync(Guid parentId);
        Task<List<ReportResponseDto>> GetReportsByTutorIdAsync(Guid tutorId);
    }
}
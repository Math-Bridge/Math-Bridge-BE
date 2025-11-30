using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IReportRepository
    {
        // Basic CRUD
        Task AddAsync(Report report);
        Task UpdateAsync(Report report);
        Task DeleteAsync(Report report);
        Task<Report?> GetByIdAsync(Guid reportId);
        Task<List<Report>> GetAllAsync();

        // Query Methods
        Task<List<Report>> GetByParentIdAsync(Guid parentId);
        Task<List<Report>> GetByTutorIdAsync(Guid tutorId);
        Task<List<Report>> GetByStatusAsync(string status);
        Task<Report?> GetLatestReportByParentIdAsync(Guid parentId);
    }
}
using MathBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MathBridge.Domain.Interfaces
{
    public interface ICenterRepository
    {
        Task AddAsync(Center center);
        Task UpdateAsync(Center center);
        Task DeleteAsync(Guid id); // Added
        Task<Center?> GetByIdAsync(Guid id);
        Task<List<Center>> GetAllAsync();
        Task<List<Center>> GetByCityAsync(string city);
        Task<List<Center>> GetByCoordinates(double latitude, double longitude, double radiusKm);
        Task<bool> ExistsAsync(Guid id);
        Task UpdateTutorCountAsync(Guid centerId, int increment);
    }
}
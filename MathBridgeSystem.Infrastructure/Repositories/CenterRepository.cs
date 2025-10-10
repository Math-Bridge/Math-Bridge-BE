using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class CenterRepository : ICenterRepository
    {
        private readonly MathBridgeDbContext _context;

        public CenterRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(Center center)
        {
            await _context.Centers.AddAsync(center);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Center center)
        {
            _context.Centers.Update(center);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var center = await _context.Centers.FindAsync(id);
            if (center != null)
            {
                _context.Centers.Remove(center);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Center?> GetByIdAsync(Guid id)
        {
            return await _context.Centers.FindAsync(id);
        }

        public async Task<List<Center>> GetAllAsync()
        {
            return await _context.Centers
                .OrderBy(c => c.City)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Center>> GetByCityAsync(string city)
        {
            return await _context.Centers
                .Where(c => c.City == city)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Center>> GetByCoordinates(double latitude, double longitude, double radiusKm)
        {
            // Tính bounding box để filter ở database level
            const double EARTH_RADIUS_KM = 6371;
            var latDelta = (radiusKm / EARTH_RADIUS_KM) * (180.0 / Math.PI);
            var lonDelta = latDelta / Math.Cos(latitude * (Math.PI / 180.0));

            var minLat = latitude - latDelta;
            var maxLat = latitude + latDelta;
            var minLon = longitude - lonDelta;
            var maxLon = longitude + lonDelta;

            // Filter ở database với bounding box
            var candidateCenters = await _context.Centers
                .Where(c => c.Latitude != null && c.Longitude != null &&
                           c.Latitude >= minLat && c.Latitude <= maxLat &&
                           c.Longitude >= minLon && c.Longitude <= maxLon)
                .ToListAsync();

            // Precise distance calculation ở client-side
            var nearbyCenters = candidateCenters
                .Where(c => CalculateDistance(latitude, longitude, (double)c.Latitude, (double)c.Longitude) <= radiusKm)
                .OrderBy(c => CalculateDistance(latitude, longitude, (double)c.Latitude, (double)c.Longitude))
                .ToList();

            return nearbyCenters;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Centers.AnyAsync(c => c.CenterId == id);
        }

        public async Task UpdateTutorCountAsync(Guid centerId, int increment)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center != null)
            {
                center.TutorCount += increment;
                if (center.TutorCount < 0) center.TutorCount = 0;
                await UpdateAsync(center);
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for distance calculation
            const double R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
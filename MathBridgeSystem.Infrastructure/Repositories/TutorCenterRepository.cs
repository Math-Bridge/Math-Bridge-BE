using MathBridge.Domain.Entities;
using MathBridge.Domain.Interfaces;
using MathBridge.Infrastructure.Data;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Infrastructure.Repositories
{
    public class TutorCenterRepository : ITutorCenterRepository
    {
        private readonly MathBridgeDbContext _context;
        private readonly ICenterRepository _centerRepository;

        public TutorCenterRepository(MathBridgeDbContext context, ICenterRepository centerRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _centerRepository = centerRepository ?? throw new ArgumentNullException(nameof(centerRepository));
        }

        public async Task AddAsync(TutorCenter tutorCenter)
        {
            await _context.TutorCenters.AddAsync(tutorCenter);
            await _context.SaveChangesAsync();

            // Update center tutor count
            await _centerRepository.UpdateTutorCountAsync(tutorCenter.CenterId, 1);
        }

        public async Task RemoveAsync(Guid tutorCenterId)
        {
            var tutorCenter = await _context.TutorCenters.FindAsync(tutorCenterId);
            if (tutorCenter != null)
            {
                var centerId = tutorCenter.CenterId;
                _context.TutorCenters.Remove(tutorCenter);
                await _context.SaveChangesAsync();

                // Update center tutor count
                await _centerRepository.UpdateTutorCountAsync(centerId, -1);
            }
        }

        public async Task<List<TutorCenter>> GetByTutorIdAsync(Guid tutorId)
        {
            return await _context.TutorCenters
                .Include(tc => tc.Center)
                .Where(tc => tc.TutorId == tutorId)
                .ToListAsync();
        }

        public async Task<List<TutorCenter>> GetByCenterIdAsync(Guid centerId)
        {
            return await _context.TutorCenters
                .Include(tc => tc.Tutor)
                .Where(tc => tc.CenterId == centerId)
                .ToListAsync();
        }

        public async Task<bool> TutorIsAssignedToCenterAsync(Guid tutorId, Guid centerId)
        {
            return await _context.TutorCenters
                .AnyAsync(tc => tc.TutorId == tutorId && tc.CenterId == centerId);
        }
    }
}
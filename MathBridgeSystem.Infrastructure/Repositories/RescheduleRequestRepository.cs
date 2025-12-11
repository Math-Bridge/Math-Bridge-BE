using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class RescheduleRequestRepository : IRescheduleRequestRepository
    {
        private readonly MathBridgeDbContext _context;

        public RescheduleRequestRepository(MathBridgeDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RescheduleRequest entity)
        {
            _context.RescheduleRequests.Add(entity);
            await _context.SaveChangesAsync();
        }

        // LẤY CHI TIẾT 1 REQUEST – CẦN ĐẦY ĐỦ THÔNG TIN ĐỂ HIỂN THỊ
        public async Task<RescheduleRequest?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.RescheduleRequests

                // Thông tin buổi học gốc
                .Include(r => r.Booking)
                    .ThenInclude(s => s.Contract)
                        .ThenInclude(c => c.Package)
                .Include(r => r.Booking)
                    .ThenInclude(s => s.Tutor)

                // Thông tin hợp đồng + trẻ chính + trẻ phụ (nếu có)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.Child)                     // Bé chính
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SecondChild)               // Bé phụ (nếu hợp đồng 2 bé)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SubstituteTutor1)
                        .ThenInclude(t => t!.FinalFeedbacks)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SubstituteTutor2)
                        .ThenInclude(t => t!.FinalFeedbacks)

                // Các user liên quan
                .Include(r => r.Parent)
                .Include(r => r.RequestedTutor)
                .Include(r => r.Staff)

                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

        // LẤY TẤT CẢ REQUEST (dành cho staff)
        public async Task<IEnumerable<RescheduleRequest>> GetAllAsync()
        {
            return await _context.RescheduleRequests

                .Include(r => r.Booking)
                    .ThenInclude(s => s.Tutor)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.Child)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SecondChild)
                .Include(r => r.Parent)
                .Include(r => r.RequestedTutor)
                .Include(r => r.Staff)

                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        // LẤY REQUEST CỦA 1 PHỤ HUYNH
        public async Task<IEnumerable<RescheduleRequest>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.RescheduleRequests

                .Include(r => r.Booking)
                    .ThenInclude(s => s.Tutor)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.Child)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SecondChild)
                .Include(r => r.Parent)
                .Include(r => r.RequestedTutor)
                .Include(r => r.Staff)

                .Where(r => r.ParentId == parentId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task UpdateAsync(RescheduleRequest entity)
        {
            _context.RescheduleRequests.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasPendingRequestForBookingAsync(Guid bookingId)
        {
            return await _context.RescheduleRequests
                .AnyAsync(r => r.BookingId == null ? false : r.BookingId == bookingId && r.Status == "pending");
        }

        public async Task<RescheduleRequest?> GetPendingRequestForBookingAsync(Guid bookingId)
        {
            return await _context.RescheduleRequests
                .FirstOrDefaultAsync(r => r.BookingId == bookingId && r.Status == "pending");
        }

        public async Task<bool> HasPendingRequestInContractAsync(Guid contractId)
        {
            return await _context.RescheduleRequests
                .AnyAsync(r => r.ContractId == contractId && r.Status == "pending");
        }
    }
}
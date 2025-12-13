using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class ContractRepository : IContractRepository
    {
        private readonly MathBridgeDbContext _context;

        public ContractRepository(MathBridgeDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Contract contract)
        {
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Contract contract)
        {
            _context.Contracts.Update(contract);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Contract>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.SecondChild)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.ContractSchedules)
                .Where(c => c.ParentId == parentId)
                .ToListAsync();
        }

        public async Task<Contract?> GetByIdAsync(Guid id)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.SecondChild)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.ContractSchedules)
                .FirstOrDefaultAsync(c => c.ContractId == id);
        }

        public async Task<Contract?> GetByIdWithPackageAsync(Guid contractId)
        {
            return await _context.Contracts
                .Include(c => c.Package)
                .Include(c => c.ContractSchedules)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);
        }

        public async Task<List<Contract>> GetAllWithDetailsAsync()
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.SecondChild)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.ContractSchedules)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Contract>> GetByParentPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.SecondChild)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.ContractSchedules)
                .Include(c => c.Sessions)
                .Include(c => c.RescheduleRequests)
                .Include(c => c.WalletTransactions)
                .Where(c => c.Parent.PhoneNumber == phoneNumber.Trim())
                .Where(c => c.Child.Status != "deleted")
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<User>> GetAvailableTutorsForContractAsync(Guid contractId)
        {
            var contract = await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.ContractSchedules)
                .FirstOrDefaultAsync(c => c.ContractId == contractId)
                ?? throw new KeyNotFoundException($"Contract {contractId} not found");

            if (!contract.ContractSchedules.Any())
                throw new InvalidOperationException("Contract has no schedule defined.");

            var maxContractsSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "MaxContractsPerTutor");

            int maxContracts = maxContractsSetting != null && int.TryParse(maxContractsSetting.Value, out int val) ? val : 4;

            var tutors = await _context.Users
                .Include(u => u.FinalFeedbacks)
                .Include(u => u.Role)
                .Include(u => u.TutorCenters)
                .Include(u => u.ContractMainTutors)
                    .ThenInclude(c => c.ContractSchedules)
                .Where(u => u.Role.RoleName == "tutor" && u.Status == "active" && u.TutorCenters.Any())
                .ToListAsync();

            var availableTutors = new List<User>();

            foreach (var tutor in tutors)
            {
                var activeContracts = tutor.ContractMainTutors
                    .Where(c => c.Status == "active")
                    .ToList();

                if (activeContracts.Count >= maxContracts) 
                    continue;

                bool hasOverlap = activeContracts.Any(existing =>
                    HasScheduleOverlap(contract.ContractSchedules, existing.ContractSchedules) &&
                    HasDateOverlap(contract.StartDate, contract.EndDate, existing.StartDate, existing.EndDate));

                if (hasOverlap) 
                    continue;

                if (contract.IsOnline)
                {
                    availableTutors.Add(tutor);
                }
                else
                {
                    var childCenterId = contract.Child?.CenterId;
                    if (childCenterId.HasValue && tutor.TutorCenters.Any(tc => tc.CenterId == childCenterId.Value))
                    {
                        availableTutors.Add(tutor);
                    }
                }
            }

            return availableTutors;
        }

        private bool HasScheduleOverlap(ICollection<ContractSchedule> s1, ICollection<ContractSchedule> s2)
        {
            foreach (var a in s1)
                foreach (var b in s2)
                {
                    if (a.DayOfWeek != b.DayOfWeek)
                        continue;

                    // Kiểm tra xem 2 buổi có overlap giờ không
                    bool hasTimeOverlap = a.StartTime < b.EndTime && b.StartTime < a.EndTime;
                    if (hasTimeOverlap)
                        return true; // Có overlap → trùng → không cho phép

                    // Nếu không overlap, tính khoảng cách giữa 2 buổi (gap)
                    TimeSpan gap;
                    if (a.EndTime <= b.StartTime)
                    {
                        // Buổi a kết thúc trước khi buổi b bắt đầu
                        gap = b.StartTime - a.EndTime;
                    }
                    else if (b.EndTime <= a.StartTime)
                    {
                        // Buổi b kết thúc trước khi buổi a bắt đầu
                        gap = a.StartTime - b.EndTime;
                    }
                    else
                    {
                        continue; // Không thể đến đây nếu không overlap, nhưng để an toàn
                    }

                    // Nếu khoảng nghỉ giữa 2 buổi < 90 phút → coi như trùng (không đủ thời gian nghỉ)
                    if (gap.TotalMinutes < 90)
                        return true;
                }

            return false;
        }
        private bool HasDateOverlap(DateOnly s1, DateOnly e1, DateOnly s2, DateOnly e2)
            => s1 <= e2 && s2 <= e1;

        public async Task<bool> HasOverlappingContractForChildAsync(
            Guid childId,
            DateOnly startDate,
            DateOnly endDate,
            List<ContractSchedule> newSchedules,
            Guid? excludeContractId = null)
        {
            var childContracts = await _context.Contracts
                .Include(c => c.ContractSchedules)
                .Where(c => (c.ChildId == childId || c.SecondChildId == childId)
                            && c.Status != "cancelled" && c.Status != "completed")
                .Where(c => excludeContractId == null || c.ContractId != excludeContractId)
                .ToListAsync();

            foreach (var existing in childContracts)
            {
                if (!HasDateOverlap(startDate, endDate, existing.StartDate, existing.EndDate))
                    continue;

                if (HasScheduleOverlap(newSchedules, existing.ContractSchedules))
                    return true;
            }

            return false;
        }

        // Nhận trực tiếp Contract object (dùng cho check trước khi tạo)
        public async Task<List<User>> GetAvailableTutorsForContractAsync(Contract contract)
        {
            // Reuse logic cũ nhưng lấy thông tin từ contract object thay vì contractId
            var child = await _context.Children
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.ChildId == contract.ChildId)
                ?? throw new KeyNotFoundException("Child not found.");

            var childCenterId = child.CenterId;

            if (!contract.IsOnline && !childCenterId.HasValue)
                throw new InvalidOperationException("Offline contract requires child to have a center.");

            var maxContractsSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "MaxContractsPerTutor");
            int maxContracts = maxContractsSetting != null && int.TryParse(maxContractsSetting.Value, out int val) ? val : 5;

            var tutors = await _context.Users
                .Include(u => u.FinalFeedbacks)
                .Include(u => u.Role)
                .Include(u => u.TutorCenters)
                .Include(u => u.ContractMainTutors)
                    .ThenInclude(c => c.ContractSchedules)
                .Where(u => u.Role.RoleName == "tutor" && u.Status == "active" && u.TutorCenters.Any())
                .ToListAsync();

            var availableTutors = new List<User>();

            foreach (var tutor in tutors)
            {
                var activeContracts = tutor.ContractMainTutors
                    .Where(c => c.Status == "active")
                    .ToList();

                if (activeContracts.Count >= maxContracts)
                    continue;

                bool hasOverlap = activeContracts.Any(existing =>
                    HasScheduleOverlap(contract.ContractSchedules, existing.ContractSchedules) &&
                    HasDateOverlap(contract.StartDate, contract.EndDate, existing.StartDate, existing.EndDate));

                if (hasOverlap)
                    continue;

                // Chỉ tutor cùng trung tâm với học sinh khi offline
                if (!contract.IsOnline)
                {
                    if (!tutor.TutorCenters.Any(tc => tc.CenterId == childCenterId.Value))
                        continue;
                }

                availableTutors.Add(tutor);
            }

            return availableTutors;
        }
    }
}
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
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.Schedules)
                .Where(c => c.ParentId == parentId)
                .ToListAsync();
        }

        public async Task<Contract?> GetByIdAsync(Guid id)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ContractId == id);
        }

        public async Task<Contract?> GetByIdWithPackageAsync(Guid contractId)
        {
            return await _context.Contracts
                .Include(c => c.Package)
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);
        }

        public async Task<List<Contract>> GetAllWithDetailsAsync()
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.Schedules)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Contract>> GetByParentPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.SubstituteTutor1)
                .Include(c => c.SubstituteTutor2)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Include(c => c.Schedules)
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
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ContractId == contractId)
                ?? throw new KeyNotFoundException($"Contract {contractId} not found");

            if (!contract.Schedules.Any())
                throw new InvalidOperationException("Contract has no schedule defined.");

            var maxContractsSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "MaxContractsPerTutor");
            int maxContracts = maxContractsSetting != null && int.TryParse(maxContractsSetting.Value, out int val) ? val : 5;

            var tutors = await _context.Users
                .Include(u => u.FinalFeedbacks)
                .Include(u => u.Role)
                .Include(u => u.TutorCenters)
                .Include(u => u.ContractMainTutors)
                    .ThenInclude(c => c.Schedules)
                .Where(u => u.Role.RoleName == "tutor" && u.Status == "active" && u.TutorCenters.Any())
                .ToListAsync();

            var availableTutors = new List<User>();

            foreach (var tutor in tutors)
            {
                var activeContracts = tutor.ContractMainTutors
                    .Where(c => c.Status == "active")
                    .ToList();

                if (activeContracts.Count >= maxContracts) continue;

<<<<<<< Updated upstream
                // Validate required properties
                if (!inputContract.DaysOfWeeks.HasValue)
                    throw new InvalidOperationException("Contract must have DaysOfWeeks defined.");
                if (!inputContract.StartTime.HasValue)
                    throw new InvalidOperationException("Contract must have StartTime defined.");
                if (!inputContract.EndTime.HasValue)
                    throw new InvalidOperationException("Contract must have EndTime defined.");

                // Get all active tutors who are assigned to at least one center
                var tutors = await _context.Users
                    .Include(u => u.FinalFeedbacks)
                    .Include(u => u.Role)
                    .Include(u => u.TutorCenters)
                    .Include(u => u.ContractMainTutors)
                    .Include(u => u.ContractSubstituteTutor1s)
                    .Include(u => u.ContractSubstituteTutor2s)
                    .Where(u => u.Role.RoleName == "tutor"
                             && u.Status == "active"
                             && u.TutorCenters.Any())
                    .ToListAsync();

                var availableTutors = new List<User>();
                var inputDaysOfWeeks = inputContract.DaysOfWeeks.Value;

                foreach (var tutor in tutors)
                {
                    // Get all active contracts of the tutor (MainTutor only)
                    var tutorActiveContracts = tutor.ContractMainTutors
                        .Where(c => c.Status == "active")
                        .ToList();

                    bool hasOverlap = false;
                    foreach (var existingContract in tutorActiveContracts)
                    {
                        if (CheckOverlap(inputContract, existingContract))
                        {
                            hasOverlap = true;
                            break;
                        }
                    }

                    if (hasOverlap) continue;

                    // Conditions according to class type
                    if (inputContract.IsOnline)
                    {
                        // Online: only tutors with centers (filtered above)
=======
                bool hasOverlap = activeContracts.Any(existing =>
                    HasScheduleOverlap(contract.Schedules, existing.Schedules) &&
                    HasDateOverlap(contract.StartDate, contract.EndDate, existing.StartDate, existing.EndDate));

                if (hasOverlap) continue;

                if (contract.IsOnline)
                {
                    availableTutors.Add(tutor);
                }
                else
                {
                    var childCenterId = contract.Child?.CenterId;
                    if (childCenterId.HasValue &&
                        tutor.TutorCenters.Any(tc => tc.CenterId == childCenterId.Value))
                    {
>>>>>>> Stashed changes
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
                    if (a.DayOfWeek == b.DayOfWeek &&
                        a.StartTime < b.EndTime && b.StartTime < a.EndTime)
                    {
                        var gap = a.StartTime > b.EndTime
                            ? a.StartTime - b.EndTime
                            : b.StartTime - a.EndTime;
                        if (gap.TotalMinutes < 90)
                            return true;
                    }
            return false;
        }

        private bool HasDateOverlap(DateOnly s1, DateOnly e1, DateOnly s2, DateOnly e2)
            => s1 <= e2 && s2 <= e1;

        // ĐÃ SỬA: Dùng entity ContractSchedule thay vì DTO
        public async Task<bool> HasOverlappingContractForChildAsync(
            Guid childId,
            DateOnly startDate,
            DateOnly endDate,
            List<ContractSchedule> newSchedules,
            Guid? excludeContractId = null)
        {
            var childContracts = await _context.Contracts
                .Include(c => c.Schedules)
                .Where(c => (c.ChildId == childId || c.SecondChildId == childId)
                            && c.Status != "cancelled" && c.Status != "completed")
                .Where(c => excludeContractId == null || c.ContractId != excludeContractId)
                .ToListAsync();

            foreach (var existing in childContracts)
            {
<<<<<<< Updated upstream
                // Get all active contracts for this child (excluding cancelled ones)
                var childContracts = await _context.Contracts
                    .Where(c => c.ChildId == childId)
                    .Where(c => c.Status != "cancelled" && c.Status != "completed")
                    .ToListAsync();
=======
                if (!HasDateOverlap(startDate, endDate, existing.StartDate, existing.EndDate))
                    continue;
>>>>>>> Stashed changes

                if (HasScheduleOverlap(newSchedules, existing.Schedules))
                    return true;
            }

            return false;
        }
    }
}
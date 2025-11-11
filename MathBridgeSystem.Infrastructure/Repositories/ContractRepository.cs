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
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Where(c => c.ParentId == parentId)
                .ToListAsync();
        }

        public async Task<Contract?> GetByIdAsync(Guid id)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.ContractId == id);
        }

        public async Task<Contract?> GetByIdWithPackageAsync(Guid contractId)
        {
            return await _context.Contracts
                .Include(c => c.Package)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);
        }
        public async Task<List<Contract>> GetAllWithDetailsAsync()
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.Package)
                .Include(c => c.Center)
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
                .Include(c => c.Sessions)
                .Include(c => c.RescheduleRequests)
                .Include(c => c.WalletTransactions)
                .Where(c => c.Parent.PhoneNumber == phoneNumber.Trim())
                .Where(c => c.Child.Status != "deleted")
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<AvailableTutorResponse>> GetAvailableTutorsForContractAsync(int contractId)
        {
            var inputContract = await _context.Contracts.FindAsync(contractId);
            if (inputContract == null)
            {
                throw new ArgumentException("Contract not found.");
            }

            var tutors = await _context.Users
                .Where(u => u.Role == "Tutor")
                .Include(u => u.ContractsAsMainTutor)
                .Include(u => u.ContractsAsSubstituteTutor1)
                .Include(u => u.ContractsAsSubstituteTutor2)
                .Include(u => u.Reviews)
                .ToListAsync();

            var availableTutors = new List<AvailableTutorResponse>();

            foreach (var tutor in tutors)
            {
                var allContracts = tutor.ContractsAsMainTutor
                    .Concat(tutor.ContractsAsSubstituteTutor1)
                    .Concat(tutor.ContractsAsSubstituteTutor2);

                var hasOverlap = allContracts.Any(c =>
                    (c.DaysOfWeeks & inputContract.DaysOfWeeks) > 0 &&
                    c.StartTime < inputContract.EndTime &&
                    c.EndTime > inputContract.StartTime &&
                    c.StartDate <= inputContract.EndDate &&
                    c.EndDate >= inputContract.StartDate &&
                    !(inputContract.StartTime >= c.EndTime.Add(TimeSpan.FromMinutes(90)))
                );

                if (!hasOverlap)
                {
                    availableTutors.Add(new AvailableTutorResponse
                    {
                        UserId = tutor.Id,
                        FullName = tutor.FullName,
                        Email = tutor.Email,
                        PhoneNumber = tutor.PhoneNumber,
                        AverageRating = tutor.Reviews.Any() ? tutor.Reviews.Average(r => r.Rating) : 0,
                        ReviewCount = tutor.Reviews.Count
                    });
                }
            }

            return availableTutors
                .OrderByDescending(t => t.AverageRating)
                .ThenByDescending(t => t.ReviewCount)
                .ToList();
        }
    }
}
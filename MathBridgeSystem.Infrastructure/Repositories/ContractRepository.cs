﻿using MathBridgeSystem.Domain.Entities;
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

        public async Task<List<User>> GetAvailableTutorsForContractAsync(Guid contractId)
        {
            try
            {
                // Get the input contract with all required properties
                var inputContract = await _context.Contracts
                    .FirstOrDefaultAsync(c => c.ContractId == contractId);

                if (inputContract == null)
                    throw new KeyNotFoundException($"Contract with ID {contractId} not found.");

                // Validate required properties
                if (!inputContract.DaysOfWeeks.HasValue)
                    throw new InvalidOperationException("Contract must have DaysOfWeeks defined.");
                if (!inputContract.StartTime.HasValue)
                    throw new InvalidOperationException("Contract must have StartTime defined.");
                if (!inputContract.EndTime.HasValue)
                    throw new InvalidOperationException("Contract must have EndTime defined.");

                // Get all tutors (Users with Tutor role) - RoleId 3 is typically tutor
                var tutors = await _context.Users
                    .Include(u => u.FinalFeedbacks)
                    .Include(u => u.Role)
                    .Include(u => u.ContractMainTutors)
                    .Include(u => u.ContractSubstituteTutor1s)
                    .Include(u => u.ContractSubstituteTutor2s)
                    .Where(u => u.Role.RoleName == "tutor" && u.Status != "inactive" && u.TutorCenters.Count >0)
                    .ToListAsync();

                var inputDaysOfWeeks = inputContract.DaysOfWeeks.Value;

                // Filter out tutors with overlapping contracts
                var availableTutors = new List<User>();

                foreach (var tutor in tutors)
                {
                    // Get all contracts where tutor is MainTutor, SubstituteTutor1, or SubstituteTutor2
                    var tutorContracts = new List<Contract>();
                    tutorContracts.AddRange(tutor.ContractMainTutors.Where(c => c.Status != "cancelled" && c.Status != "completed"));
                    tutorContracts.AddRange(tutor.ContractSubstituteTutor1s.Where(c => c.Status != "cancelled" && c.Status != "completed"));
                    tutorContracts.AddRange(tutor.ContractSubstituteTutor2s.Where(c => c.Status != "cancelled" && c.Status != "completed"));

                    bool hasOverlap = false;

                    foreach (var existingContract in tutorContracts)
                    {
                        // Check if ALL overlap conditions are met
                        if (CheckOverlap(inputContract, existingContract))
                        {
                            hasOverlap = true;
                            break;
                        }
                    }

                    if (!hasOverlap)
                    {
                        availableTutors.Add(tutor);
                    }
                }

                return availableTutors;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving available tutors: {ex.Message}", ex);
            }
        }

        private bool CheckOverlap(Contract inputContract, Contract existingContract)
        {
            try
            {
                // Condition 1: Check days of week overlap (bitmask)
                if (!inputContract.DaysOfWeeks.HasValue || !existingContract.DaysOfWeeks.HasValue)
                    return false;

                var daysOverlap = (inputContract.DaysOfWeeks.Value & existingContract.DaysOfWeeks.Value) > 0;
                if (!daysOverlap)
                    return false;

                // Condition 2: Check time range overlap
                if (!inputContract.StartTime.HasValue || !inputContract.EndTime.HasValue ||
                    !existingContract.StartTime.HasValue || !existingContract.EndTime.HasValue)
                    return false;

                var timeOverlap = TimeRangesOverlap(
                    inputContract.StartTime.Value,
                    inputContract.EndTime.Value,
                    existingContract.StartTime.Value,
                    existingContract.EndTime.Value);

                if (!timeOverlap)
                    return false;

                // Condition 3: Check date range overlap
                var dateOverlap = DateRangesOverlap(
                    inputContract.StartDate,
                    inputContract.EndDate,
                    existingContract.StartDate,
                    existingContract.EndDate);

                if (!dateOverlap)
                    return false;

                // All overlap conditions met - check for the 1h30m exception rule
                // If input contract's StartTime is at least 90 minutes after existing contract's EndTime, no overlap
                var gapInMinutes = (inputContract.StartTime.Value.ToTimeSpan() - existingContract.EndTime.Value.ToTimeSpan()).TotalMinutes;
                if (gapInMinutes >= 90)
                    return false; // Exception applies - no overlap

                // All conditions met and no exception applies - there is overlap
                return true;
            }
            catch
            {
                // If any error occurs during overlap checking, assume no overlap for safety
                return false;
            }
        }

        private bool TimeRangesOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
        {
            // Convert to TimeSpan for comparison
            var span1Start = start1.ToTimeSpan();
            var span1End = end1.ToTimeSpan();
            var span2Start = start2.ToTimeSpan();
            var span2End = end2.ToTimeSpan();

            // Check if ranges overlap
            return span1Start < span2End && span2Start < span1End;
        }

        private bool DateRangesOverlap(DateOnly start1, DateOnly end1, DateOnly start2, DateOnly end2)
        {
            // Check if date ranges overlap
            return start1 <= end2 && start2 <= end1;
        }

        public async Task<bool> HasOverlappingContractForChildAsync(
            Guid childId, 
            DateOnly startDate, 
            DateOnly endDate, 
            TimeOnly? startTime, 
            TimeOnly? endTime, 
            byte? daysOfWeeks, 
            Guid? excludeContractId = null)
        {
            try
            {
                // Get all active contracts for this child (excluding cancelled ones)
                var childContracts = await _context.Contracts
                    .Where(c => c.ChildId == childId)
                    .Where(c => c.Status != "cancelled" && c.Status != "completed")
                    .ToListAsync();

                // If excludeContractId is provided, filter it out (used for updates)
                if (excludeContractId.HasValue)
                {
                    childContracts = childContracts.Where(c => c.ContractId != excludeContractId.Value).ToList();
                }

                // If no active contracts exist, no overlap
                if (!childContracts.Any())
                    return false;

                // Create a temporary contract object for overlap checking
                var newContract = new Contract
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    DaysOfWeeks = daysOfWeeks
                };

                // Check for overlap with each existing contract
                foreach (var existingContract in childContracts)
                {
                    if (CheckOverlap(newContract, existingContract))
                    {
                        return true; // Overlap found
                    }
                }

                return false; // No overlap
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error checking for overlapping contracts: {ex.Message}", ex);
            }
        }
    }
}


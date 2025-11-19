using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class TestResultRepository : ITestResultRepository
    {
        private readonly MathBridgeDbContext _context;

        public TestResultRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<TestResult> GetByIdAsync(Guid id)
        {
            return await _context.TestResults
                .Include(t => t.Contract).Include(t => t.Booking)
                .FirstOrDefaultAsync(t => t.ResultId == id);
        }

        public async Task<IEnumerable<TestResult>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.TestResults
                .Include(t => t.Contract)
                .Where(t => t.ContractId == contractId)
                .ToListAsync();
        }

        public async Task<TestResult> AddAsync(TestResult testResult)
        {
            _context.TestResults.Add(testResult);
            await _context.SaveChangesAsync();
            return testResult;
        }

        public async Task<TestResult> UpdateAsync(TestResult testResult)
        {
            _context.TestResults.Update(testResult);
            await _context.SaveChangesAsync();
            return testResult;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var testResult = await _context.TestResults.FirstOrDefaultAsync(t => t.ResultId == id);
            if (testResult != null)
            {
                _context.TestResults.Remove(testResult);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}

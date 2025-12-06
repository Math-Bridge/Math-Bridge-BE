using MathBridgeSystem.Application.DTOs.SessionUnitAssignment;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    /// <summary>
    /// Service for assigning units to contract sessions
    /// </summary>
    public class SessionUnitAssignmentService : ISessionUnitAssignmentService
    {
        private readonly IContractRepository _contractRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IDailyReportRepository _dailyReportRepository;

        public SessionUnitAssignmentService(
            IContractRepository contractRepository,
            ISessionRepository sessionRepository,
            IUnitRepository unitRepository,
            IDailyReportRepository dailyReportRepository)
        {
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            _dailyReportRepository = dailyReportRepository ?? throw new ArgumentNullException(nameof(dailyReportRepository));
        }

        /// <summary>
        /// Assigns units to all sessions of a contract based on daily reports
        /// </summary>
        public async Task<AssignUnitsToContractSessionsResponse> AssignUnitsToContractSessionsAsync(Guid contractId)
        {
            // Validate contract exists
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null)
                throw new KeyNotFoundException($"Contract with ID {contractId} not found.");

            // Get all sessions for the contract, ordered by date
            var sessions = await _sessionRepository.GetByContractIdAsync(contractId);
            if (sessions == null || sessions.Count == 0)
                throw new InvalidOperationException($"No sessions found for contract {contractId}.");

            var orderedSessions = sessions.OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime).ToList();

            // Get units for the contract's curriculum, ordered by UnitOrder
            var units = await _unitRepository.GetByContractIdAsync(contractId);
            if (units == null || units.Count == 0)
                throw new InvalidOperationException($"No units found for contract {contractId}. Please ensure the contract's package has an associated curriculum with units.");

            var orderedUnits = units.Where(u => u.IsActive).OrderBy(u => u.UnitOrder).ToList();
            if (orderedUnits.Count == 0)
                throw new InvalidOperationException($"No active units found for contract {contractId}.");

            // Get all daily reports for the contract's sessions
            var sessionIds = orderedSessions.Select(s => s.BookingId).ToList();
            var dailyReports = await _dailyReportRepository.GetByBookingIdsAsync(sessionIds);

            // Determine starting unit index
            int startingUnitIndex = 0;
            int startingUnitOrder = orderedUnits.First().UnitOrder;

            if (dailyReports != null && dailyReports.Any())
            {
                // Find the oldest daily report to get the starting unit
                var oldestReport = dailyReports.OrderBy(d => d.CreatedDate).First();
                var oldestReportUnit = orderedUnits.FirstOrDefault(u => u.UnitId == oldestReport.UnitId);

                if (oldestReportUnit != null)
                {
                    // Start from the unit of the oldest daily report
                    startingUnitIndex = orderedUnits.FindIndex(u => u.UnitId == oldestReportUnit.UnitId);
                    startingUnitOrder = oldestReportUnit.UnitOrder;
                }
            }

            // Assign units to sessions
            var assignmentDetails = new List<SessionUnitAssignmentDetail>();
            int currentUnitIndex = startingUnitIndex;
            int sessionsAssigned = 0;
            int sessionsWithoutUnit = 0;

            foreach (var session in orderedSessions)
            {
                SessionUnitAssignmentDetail detail;

                if (currentUnitIndex < orderedUnits.Count)
                {
                    // Assign current unit to session
                    var unit = orderedUnits[currentUnitIndex];
                    session.UnitId = unit.UnitId;
                    session.UpdatedAt = DateTime.UtcNow.ToLocalTime();

                    detail = new SessionUnitAssignmentDetail
                    {
                        BookingId = session.BookingId,
                        SessionDate = session.SessionDate,
                        UnitId = unit.UnitId,
                        UnitName = unit.UnitName,
                        UnitOrder = unit.UnitOrder
                    };

                    currentUnitIndex++;
                    sessionsAssigned++;
                }
                else
                {
                    // No more units available, leave UnitId as null
                    session.UnitId = null;
                    session.UpdatedAt = DateTime.UtcNow.ToLocalTime();

                    detail = new SessionUnitAssignmentDetail
                    {
                        BookingId = session.BookingId,
                        SessionDate = session.SessionDate,
                        UnitId = null,
                        UnitName = null,
                        UnitOrder = null
                    };

                    sessionsWithoutUnit++;
                }

                assignmentDetails.Add(detail);
            }

            // Update all sessions in batch
            await _sessionRepository.UpdateRangeAsync(orderedSessions);

            return new AssignUnitsToContractSessionsResponse
            {
                ContractId = contractId,
                TotalSessions = orderedSessions.Count,
                SessionsAssigned = sessionsAssigned,
                SessionsWithoutUnit = sessionsWithoutUnit,
                StartingUnitOrder = startingUnitOrder,
                AssignmentDetails = assignmentDetails
            };
        }
    }
}

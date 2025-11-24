﻿using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IRescheduleRequestRepository
    {
        Task AddAsync(RescheduleRequest entity);
        Task<RescheduleRequest?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<RescheduleRequest>> GetAllAsync();
        Task<IEnumerable<RescheduleRequest>> GetByParentIdAsync(Guid parentId);
        Task UpdateAsync(RescheduleRequest entity);
        Task<bool> HasPendingRequestForBookingAsync(Guid bookingId);
        Task<RescheduleRequest?> GetPendingRequestForBookingAsync(Guid bookingId);
        Task<bool> HasPendingRequestInContractAsync(Guid contractId);
    }
}
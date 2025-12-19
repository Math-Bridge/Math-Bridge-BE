using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IRescheduleService
    {
        Task<RescheduleResponseDto> CreateRequestAsync(Guid parentId, CreateRescheduleRequestDto dto);
        Task<RescheduleResponseDto> ApproveRequestAsync(Guid staffId, Guid requestId, ApproveRescheduleRequestDto dto);
        Task<RescheduleResponseDto> RejectRequestAsync(Guid staffId, Guid requestId, string reason);
        Task<AvailableSubTutorsDto> GetAvailableSubTutorsAsync(Guid rescheduleRequestId);
        Task<RescheduleRequestDto?> GetByIdAsync(Guid requestId, Guid userId, string role);
        Task<IEnumerable<RescheduleRequestDto>> GetAllAsync(Guid? parentId = null);
        Task<RescheduleResponseDto> CancelSessionAndRefundAsync(Guid sessionId, Guid rescheduleRequestId );
        Task<object> CreateTutorReplacementRequestAsync(Guid bookingId, Guid tutorId, string reason);
        Task<RescheduleResponseDto> CreateMakeUpSessionRequestAsync(Guid parentId, CreateRescheduleRequestDto dto);
        Task<IEnumerable<RescheduleRequestDto>> GetByTutorIdAsync(Guid tutorId);
    }
}
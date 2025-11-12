using MathBridgeSystem.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IRescheduleService
    {
        Task<RescheduleResponseDto> CreateRequestAsync(Guid parentId, CreateRescheduleRequestDto dto);
        Task<RescheduleResponseDto> ApproveRequestAsync(Guid staffId, Guid requestId, ApproveRescheduleRequestDto dto);
        Task<RescheduleResponseDto> RejectRequestAsync(Guid staffId, Guid requestId, string reason);
        Task<AvailableSubTutorsDto> GetAvailableSubTutorsAsync(Guid rescheduleRequestId);
    }
}
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly ISupportRequestRepository _supportRequestRepository;
        private readonly IUserRepository _userRepository;

        public SupportRequestService(ISupportRequestRepository supportRequestRepository, IUserRepository userRepository)
        {
            _supportRequestRepository = supportRequestRepository ?? throw new ArgumentNullException(nameof(supportRequestRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<Guid> CreateSupportRequestAsync(CreateSupportRequestRequest request, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Category))
                throw new ArgumentException("Subject, description, and category are required.");

            if (!await _userRepository.ExistsAsync(userId))
                throw new InvalidOperationException("User not found.");

            var supportRequest = new SupportRequest
            {
                RequestId = Guid.NewGuid(),
                UserId = userId,
                Subject = request.Subject.Trim(),
                Description = request.Description.Trim(),
                Category = request.Category.Trim(),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                UpdatedDate = DateTime.UtcNow.ToLocalTime()
            };
            await _supportRequestRepository.AddAsync(supportRequest);
            return supportRequest.RequestId;
        }

        public async Task UpdateSupportRequestAsync(Guid id, UpdateSupportRequestRequest request, Guid userId)
        {
            var supportRequest = await _supportRequestRepository.GetByIdAsync(id);
            if (supportRequest == null)
                throw new InvalidOperationException("Support request not found.");
            if (supportRequest.UserId != userId)
                throw new UnauthorizedAccessException();

            supportRequest.Subject = request.Subject.Trim();
            supportRequest.Description = request.Description.Trim();
            supportRequest.Category = request.Category.Trim();
            supportRequest.UpdatedDate = DateTime.UtcNow.ToLocalTime();

            await _supportRequestRepository.UpdateAsync(supportRequest);
        }

        public async Task DeleteSupportRequestAsync(Guid id, Guid userId)
        {
            var supportRequest = await _supportRequestRepository.GetByIdAsync(id);
            if (supportRequest == null)
                throw new InvalidOperationException("Support request not found.");
            if (supportRequest.UserId != userId)
                throw new UnauthorizedAccessException();

            await _supportRequestRepository.DeleteAsync(id);
        }

        public async Task<SupportRequestDto?> GetSupportRequestByIdAsync(Guid id)
        {
            var sr = await _supportRequestRepository.GetByIdAsync(id);
            return sr == null ? null : MapToDto(sr);
        }

        public async Task<List<SupportRequestDto>> GetAllSupportRequestsAsync()
        {
            var list = await _supportRequestRepository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<SupportRequestDto>> GetSupportRequestsByUserIdAsync(Guid userId)
        {
            var list = await _supportRequestRepository.GetByUserIdAsync(userId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<SupportRequestDto>> GetSupportRequestsByStatusAsync(string status)
        {
            var list = await _supportRequestRepository.GetByStatusAsync(status);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<SupportRequestDto>> GetSupportRequestsByCategoryAsync(string category)
        {
            var list = await _supportRequestRepository.GetByCategoryAsync(category);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<SupportRequestDto>> GetSupportRequestsByAssignedUserIdAsync(Guid assignedUserId)
        {
            var list = await _supportRequestRepository.GetByAssignedUserIdAsync(assignedUserId);
            return list.Select(MapToDto).ToList();
        }

        public async Task AssignSupportRequestAsync(Guid id, AssignSupportRequestRequest request)
        {
            var sr = await _supportRequestRepository.GetByIdAsync(id);
            if (sr == null)
                throw new InvalidOperationException("Support request not found.");
            if (!await _userRepository.ExistsAsync(request.AssignedToUserId))
                throw new InvalidOperationException("Assigned user not found.");

            sr.AssignedToUserId = request.AssignedToUserId;
            sr.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            await _supportRequestRepository.UpdateAsync(sr);
        }

        public async Task UpdateSupportRequestStatusAsync(Guid id, UpdateSupportRequestStatusRequest request)
        {
            var sr = await _supportRequestRepository.GetByIdAsync(id);
            if (sr == null)
                throw new InvalidOperationException("Support request not found.");

            sr.Status = request.Status.Trim();
            sr.Resolution = request.Resolution?.Trim();
            sr.AdminNotes = request.AdminNotes?.Trim();
            sr.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            if (string.Equals(sr.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
                sr.ResolvedDate = DateTime.UtcNow.ToLocalTime();

            await _supportRequestRepository.UpdateAsync(sr);
        }

        private SupportRequestDto MapToDto(SupportRequest sr)
        {
            return new SupportRequestDto
            {
                RequestId = sr.RequestId,
                UserId = sr.UserId,
                UserName = sr.User?.FullName ?? string.Empty,
                AssignedToUserId = sr.AssignedToUserId,
                AssignedToUserName = sr.AssignedToUser?.FullName,
                Subject = sr.Subject,
                Description = sr.Description,
                Category = sr.Category,
                Status = sr.Status,
                Resolution = sr.Resolution,
                AdminNotes = sr.AdminNotes,
                CreatedDate = sr.CreatedDate,
                UpdatedDate = sr.UpdatedDate,
                ResolvedDate = sr.ResolvedDate
            };
        }
    }
}
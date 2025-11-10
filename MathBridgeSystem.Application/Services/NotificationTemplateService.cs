using MathBridgeSystem.Application.DTOs.NotificationTemplate;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class NotificationTemplateService : INotificationTemplateService
    {
        private readonly INotificationTemplateRepository _templateRepository;

        public NotificationTemplateService(INotificationTemplateRepository templateRepository)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        }

        public async Task<NotificationTemplateDto?> GetByIdAsync(Guid templateId)
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            return template != null ? MapToDto(template) : null;
        }

        public async Task<List<NotificationTemplateDto>> GetAllAsync()
        {
            var templates = await _templateRepository.GetAllAsync();
            return templates.Select(MapToDto).ToList();
        }

        public async Task<NotificationTemplateDto?> GetByNameAsync(string name)
        {
            var template = await _templateRepository.GetByNameAsync(name);
            return template != null ? MapToDto(template) : null;
        }

        public async Task<List<NotificationTemplateDto>> GetByNotificationTypeAsync(string notificationType)
        {
            var templates = await _templateRepository.GetByNotificationTypeAsync(notificationType);
            return templates.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationTemplateDto>> GetActiveTemplatesAsync()
        {
            var templates = await _templateRepository.GetActiveTemplatesAsync();
            return templates.Select(MapToDto).ToList();
        }

        public async Task<NotificationTemplateDto> CreateAsync(CreateNotificationTemplateRequest request)
        {
            var template = new NotificationTemplate
            {
                TemplateId = Guid.NewGuid(),
                Name = request.Name,
                Subject = request.Subject,
                Body = request.Body,
                NotificationType = request.NotificationType,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            await _templateRepository.AddAsync(template);
            return MapToDto(template);
        }

        public async Task<NotificationTemplateDto?> UpdateAsync(Guid templateId, UpdateNotificationTemplateRequest request)
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                template.Name = request.Name;
            }

            if (!string.IsNullOrEmpty(request.Subject))
            {
                template.Subject = request.Subject;
            }

            if (!string.IsNullOrEmpty(request.Body))
            {
                template.Body = request.Body;
            }

            if (!string.IsNullOrEmpty(request.NotificationType))
            {
                template.NotificationType = request.NotificationType;
            }

            if (request.IsActive.HasValue)
            {
                template.IsActive = request.IsActive.Value;
            }

            await _templateRepository.UpdateAsync(template);
            return MapToDto(template);
        }

        public async Task<bool> DeleteAsync(Guid templateId)
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                return false;
            }

            await _templateRepository.DeleteAsync(templateId);
            return true;
        }

        public async Task<bool> ToggleActiveStatusAsync(Guid templateId)
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                return false;
            }

            template.IsActive = !template.IsActive;
            await _templateRepository.UpdateAsync(template);
            return true;
        }

        private NotificationTemplateDto MapToDto(NotificationTemplate template)
        {
            return new NotificationTemplateDto
            {
                TemplateId = template.TemplateId,
                Name = template.Name,
                Subject = template.Subject,
                Body = template.Body,
                NotificationType = template.NotificationType,
                IsActive = template.IsActive,
                CreatedDate = template.CreatedDate,
                UpdatedDate = template.UpdatedDate
            };
        }
    }
}
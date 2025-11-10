using MathBridgeSystem.Application.DTOs.NotificationTemplate;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationTemplateController : ControllerBase
    {
        private readonly INotificationTemplateService _templateService;

        public NotificationTemplateController(INotificationTemplateService templateService)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        }

        /// <summary>
        /// Get template by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationTemplateDto>> GetById(Guid id)
        {
            var template = await _templateService.GetByIdAsync(id);
            if (template == null)
            {
                return NotFound(new { message = "Notification template not found" });
            }
            return Ok(template);
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<NotificationTemplateDto>>> GetAll()
        {
            var templates = await _templateService.GetAllAsync();
            return Ok(templates);
        }

        /// <summary>
        /// Get template by name
        /// </summary>
        [HttpGet("name/{name}")]
        public async Task<ActionResult<NotificationTemplateDto>> GetByName(string name)
        {
            var template = await _templateService.GetByNameAsync(name);
            if (template == null)
            {
                return NotFound(new { message = "Notification template not found" });
            }
            return Ok(template);
        }

        /// <summary>
        /// Get templates by notification type
        /// </summary>
        [HttpGet("type/{notificationType}")]
        public async Task<ActionResult<List<NotificationTemplateDto>>> GetByType(string notificationType)
        {
            var templates = await _templateService.GetByNotificationTypeAsync(notificationType);
            return Ok(templates);
        }

        /// <summary>
        /// Get active templates
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<NotificationTemplateDto>>> GetActiveTemplates()
        {
            var templates = await _templateService.GetActiveTemplatesAsync();
            return Ok(templates);
        }

        /// <summary>
        /// Create a new template
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NotificationTemplateDto>> Create([FromBody] CreateNotificationTemplateRequest request)
        {
            try
            {
                var template = await _templateService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = template.TemplateId }, template);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to create template", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a template
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NotificationTemplateDto>> Update(Guid id, [FromBody] UpdateNotificationTemplateRequest request)
        {
            try
            {
                var template = await _templateService.UpdateAsync(id, request);
                if (template == null)
                {
                    return NotFound(new { message = "Notification template not found" });
                }
                return Ok(template);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to update template", error = ex.Message });
            }
        }

        /// <summary>
        /// Toggle template active status
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ToggleActiveStatus(Guid id)
        {
            var result = await _templateService.ToggleActiveStatusAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Notification template not found" });
            }
            return Ok(new { message = "Template active status toggled successfully" });
        }

        /// <summary>
        /// Delete a template
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _templateService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Notification template not found" });
            }
            return Ok(new { message = "Template deleted successfully" });
        }
    }
}
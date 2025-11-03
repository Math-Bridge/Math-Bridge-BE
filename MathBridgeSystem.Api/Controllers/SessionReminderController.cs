using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SessionReminderController : ControllerBase
    {
        private readonly ISessionReminderService _sessionReminderService;

        public SessionReminderController(ISessionReminderService sessionReminderService)
        {
            _sessionReminderService = sessionReminderService ?? throw new ArgumentNullException(nameof(sessionReminderService));
        }

        [HttpPost("trigger-24hr-reminders")]
        public async Task<ActionResult> Trigger24HourReminders()
        {
            await _sessionReminderService.CheckAndSendRemindersAsync();
            return Ok(new { message = "24-hour reminders triggered" });
        }

        [HttpPost("trigger-1hr-reminders")]
        public async Task<ActionResult> Trigger1HourReminders()
        {
            await _sessionReminderService.CheckAndSendRemindersAsync();
            return Ok(new { message = "1-hour reminders triggered" });
        }

        [HttpPost("check-and-send")]
        public async Task<ActionResult> CheckAndSendReminders()
        {
            try
            {
                await _sessionReminderService.CheckAndSendRemindersAsync();
                return Ok(new { message = "Reminders processed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing reminders", error = ex.Message });
            }
        }
    }
}
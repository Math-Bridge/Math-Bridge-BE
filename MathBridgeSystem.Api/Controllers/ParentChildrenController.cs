using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/parents/{parentId}/children")]
    [ApiController]
    [Authorize(Roles = "parent")]
    public class ParentChildrenController : ControllerBase
    {
        private readonly IChildService _childService;

        public ParentChildrenController(IChildService childService)
        {
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
        }

        [HttpPost]
        public async Task<IActionResult> AddChild(Guid parentId, [FromBody] AddChildRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var childId = await _childService.AddChildAsync(parentId, request);
                return Ok(new { childId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChildren(Guid parentId)
        {
            try
            {
                var children = await _childService.GetChildrenByParentAsync(parentId);
                return Ok(children);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/children")]
    [ApiController]
    [Authorize]
    public class ChildrenController : ControllerBase
    {
        private readonly IChildService _childService;

        public ChildrenController(IChildService childService)
        {
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> UpdateChild(Guid id, [FromBody] UpdateChildRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _childService.UpdateChildAsync(id, request);
                return Ok(new { message = "Child updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> SoftDeleteChild(Guid id)
        {
            try
            {
                await _childService.SoftDeleteChildAsync(id);
                return Ok(new { message = "Child soft deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChildById(Guid id)
        {
            try
            {
                var child = await _childService.GetChildByIdAsync(id);
                return Ok(child);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff,parent")]
        public async Task<IActionResult> GetAllChildren()
        {
            try
            {
                var children = await _childService.GetAllChildrenAsync();
                return Ok(children);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/link-center")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> LinkCenter(Guid id, [FromBody] LinkCenterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _childService.LinkCenterAsync(id, request);
                return Ok(new { message = "Center linked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}/contracts")]
        public async Task<IActionResult> GetChildContracts(Guid id)
        {
            try
            {
                var contracts = await _childService.GetChildContractsAsync(id);
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPatch("{id}/restore")]
        [Authorize(Roles = "admin,parent,staff")]
        public async Task<IActionResult> RestoreChild(Guid id)
        {
            try
            {
                await _childService.RestoreChildAsync(id);
                return Ok(new { message = "Child restored successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        /// <summary>
        /// Upload or update child profile picture
        /// </summary>
        /// <param name="id">Child ID</param>
        /// <param name="file">Image file (JPG, PNG, WebP, max 2MB)</param>
        /// <returns>URL of the uploaded avatar</returns>
        [HttpPost("{id}/avatar")]
        [Authorize(Roles = "parent,admin")]
        public async Task<IActionResult> UploadChildAvatar(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            try
            {
                var currentUserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(currentUserRole))
                {
                    Console.WriteLine("UploadChildAvatar: Role not found in token");
                    return Unauthorized(new { error = "Role not found in token" });
                }

                var command = new UpdateProfilePictureCommand
                {
                    File = file,
                    UserId = id // Using UserId field to store childId for reuse
                };

                var avatarUrl = await _childService.UpdateChildProfilePictureAsync(id, command, currentUserId, currentUserRole);
                return Ok(new { avatarUrl, message = "Child profile picture updated successfully" });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Validation error in UploadChildAvatar: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadChildAvatar: {ex}");
                var errorMessage = string.IsNullOrEmpty(ex.Message) ? "An error occurred while uploading the child profile picture." : ex.Message;
                return StatusCode(500, new { error = errorMessage });
            }
        }
    }
}
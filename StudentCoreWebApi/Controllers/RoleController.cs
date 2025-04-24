using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using StudentCoreWebApi.Response;
using System.Security.Claims;

namespace StudentCoreWebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<RoleController> _logger;

        public RoleController(IRoleRepository roleRepository, ILogger<RoleController> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            _logger.LogInformation("Fetching all roles.");
            var roles = await _roleRepository.GetRolesAsync();
            if (roles == null || roles.Count == 0)
            {
                _logger.LogWarning("No roles found.");
                return NotFound(new ApiResponse<List<Role>>(false, "No roles found.", null));
            }

            _logger.LogInformation("Roles retrieved successfully.");
            return Ok(new ApiResponse<List<Role>>(true, "Roles retrieved successfully.", roles));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] Role role)
        {
            var userId = GetUserIdFromToken();
            var result = await _roleRepository.CreateRoleAsync(userId, role);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] Role role)
        {
            var userId = GetUserIdFromToken();
            _logger.LogInformation("Updating role with ID: {RoleId} by User: {UserId}", id, userId);
            var result = await _roleRepository.UpdateRoleAsync(userId, id, role);
            if (!result.Success)
            {
                _logger.LogWarning("Role update failed: {Message}", result.Message);
                return NotFound(result);
            }

            _logger.LogInformation("Role updated successfully: {RoleId}", id);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            var userId = GetUserIdFromToken();
            _logger.LogInformation("Deleting role with ID: {RoleId} by User: {UserId}", id, userId);
            var result = await _roleRepository.DeleteRoleAsync(userId, id);
            if (!result.Success)
            {
                _logger.LogWarning("Role deletion failed: {Message}", result.Message);
                return NotFound(result);
            }

            _logger.LogInformation("Role deleted successfully: {RoleId}", id);
            return Ok(result);
        }


        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto dto)
        {
            var currentUserId = GetUserIdFromToken();
            var result = await _roleRepository.AssignRoleToUserAsync(currentUserId, dto.UserId, dto.RoleId);
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveUserRole([FromBody] AssignRoleDto dto)
        {
            var currentUserId = GetUserIdFromToken();
            var result = await _roleRepository.RemoveUserRoleAsync(currentUserId, dto.UserId, dto.RoleId);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("assigned-role")]
        public async Task<IActionResult> GetUserAssignedRole([FromBody] UserIdDto dto)
        {
            var result = await _roleRepository.GetUserAssignedRoleAsync(dto.UserId);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}

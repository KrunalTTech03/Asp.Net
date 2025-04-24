using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Model;
using System.Security.Claims;

namespace StudentCoreWebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionController(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        private Guid GetUserId() =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : Guid.Empty;

        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] Permission permission)
        {
            var userId = GetUserId();
            var result = await _permissionRepository.CreatePermissionAsync(userId, permission);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignPermissionsToRole([FromBody] AssignPermissionDto dto)
        {
            var userId = GetUserId();
            var result = await _permissionRepository.AssignPermissionsToRoleAsync(userId, dto.RoleId, dto.PermissionIds); // Modified to handle multiple permissions
            return result.Success ? Ok(result) : Unauthorized(result);
        }



        [HttpDelete("remove")]
        public async Task<IActionResult> RemovePermissionFromRole([FromBody] AssignPermissionDto dto)
        {
            var userId = GetUserId();
            var result = await _permissionRepository.RemovePermissionFromRoleAsync(userId, dto.RoleId, dto.PermissionIds.FirstOrDefault()); // Assuming you remove one permission at a time
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetPermissionsByRole(Guid roleId)
        {
            var result = await _permissionRepository.GetPermissionsByRoleAsync(roleId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("role-permissions")]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            var result = await _permissionRepository.GetAllRolePermissionsAsync();
            return Ok(result);
        }

    }

}

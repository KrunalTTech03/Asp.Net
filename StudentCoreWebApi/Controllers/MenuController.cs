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
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuRepository _menuRepository;
        private readonly ILogger<MenuController> _logger;

        public MenuController(IMenuRepository menuRepository, ILogger<MenuController> logger)
        {
            _menuRepository = menuRepository;
            _logger = logger;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllMenus()
        {
            try
            {
                _logger.LogInformation("Fetching all menus.");
                var menus = await _menuRepository.GetAllMenusAsync();

                var response = new ApiResponse<List<MenuDTO>>(true, "All menus fetched successfully.", menus);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all menus.");
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error while retrieving menus."));
            }
        }

        [HttpGet("permission-all")]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                var permissions = await _menuRepository.GetAllPermissionsAsync();
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all permissions.");
                return StatusCode(500, "Internal server error while retrieving permissions.");
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetMenuByUser(Guid userId)
        {
            try
            {
                _logger.LogInformation("Fetching menus for user with ID: {UserId}", userId);
                var menus = await _menuRepository.GetMenuByUserAsync(userId);

                var response = new ApiResponse<List<MenuDTO>>(true, "Menus fetched successfully.", menus);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching menus for user ID: {UserId}", userId);
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error while retrieving menus."));
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateMenu([FromBody] MenuDTO menuDto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("User {UserId} attempting to create menu with title: {Title}", userId, menuDto.Title);

                var result = await _menuRepository.CreateMenuAsync(userId, menuDto);
                if (!result.Success)
                {
                    _logger.LogWarning("Failed to create menu: {Message}", result.Message);
                    return Unauthorized(result);
                }

                _logger.LogInformation("Menu created successfully: {Title}", menuDto.Title);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating menu.");
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        [HttpPut("update/{menuId}")]
        public async Task<IActionResult> UpdateMenu(Guid menuId, [FromBody] MenuDTO menuDto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("User {UserId} attempting to update menu: {MenuId}", userId, menuId);

                var result = await _menuRepository.UpdateMenuAsync(userId, menuId, menuDto);
                if (!result.Success)
                {
                    if (result.Message.Contains("Access denied"))
                    {
                        _logger.LogWarning("Unauthorized menu update attempt: {Message}", result.Message);
                        return Unauthorized(result);
                    }

                    _logger.LogWarning("Failed to update menu: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Menu updated successfully: {MenuId}", menuId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating menu {MenuId}.", menuId);
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        [HttpDelete("delete/{menuId}")]
        public async Task<IActionResult> DeleteMenu(Guid menuId)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("User {UserId} attempting to delete menu: {MenuId}", userId, menuId);

                var result = await _menuRepository.DeleteMenuAsync(userId, menuId);
                if (!result.Success)
                {
                    if (result.Message.Contains("Access denied"))
                    {
                        _logger.LogWarning("Unauthorized menu deletion attempt: {Message}", result.Message);
                        return Unauthorized(result);
                    }

                    _logger.LogWarning("Failed to delete menu: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Menu deleted successfully: {MenuId}", menuId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting menu {MenuId}.", menuId);
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        [HttpGet("{menuId}/permissions")]
        public async Task<IActionResult> GetMenuPermissions(Guid menuId)
        {
            var result = await _menuRepository.GetPermissionsByMenuIdAsync(menuId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPost("assign-permission")] 
        public async Task<IActionResult> AssignPermissionToMenu([FromBody] AssignMenuPermissionDTO dto)
        {
            try
            {
                if (dto == null || dto.PermissionIds == null || dto.PermissionIds.Count == 0)
                {
                    _logger.LogWarning("Invalid assign permission request: DTO or permission IDs are null/empty");
                    return BadRequest(new ApiResponse<object>(false, "Permission IDs cannot be null or empty"));
                }

                var userId = GetUserIdFromToken();
                _logger.LogInformation("User {UserId} attempting to assign {Count} permissions to menu: {MenuId}",
                    userId, dto.PermissionIds.Count, dto.MenuId);

                var result = await _menuRepository.AssignPermissionToMenuAsync(userId, dto.MenuId, dto.RoleId,  dto.PermissionIds);
                if (!result.Success)
                {
                    if (result.Message.Contains("Access denied"))
                    {
                        _logger.LogWarning("Unauthorized permission assignment attempt: {Message}", result.Message);
                        return Unauthorized(result);
                    }

                    _logger.LogWarning("Failed to assign permissions: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Successfully assigned {Count} permissions to Menu {MenuId}",
                    dto.PermissionIds.Count, dto.MenuId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while assigning permissions to Menu {MenuId}", dto.MenuId);
                return StatusCode(500, new ApiResponse<object>(false, $"Internal server error: {ex.Message}"));
            }
        }

        [HttpPost("create-permission")]
        public async Task<IActionResult> CreatePermission([FromBody] CreateMenuPermission permission)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("User {UserId} attempting to create permission: {Name}", userId, permission.Name);

                var result = await _menuRepository.CreatePermissionAsync(userId, permission);
                if (!result.Success)
                {
                    if (result.Message.Contains("Access denied"))
                    {
                        _logger.LogWarning("Unauthorized permission creation attempt: {Message}", result.Message);
                        return Unauthorized(result);
                    }

                    _logger.LogWarning("Failed to create permission: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Permission created successfully: {Name}", permission.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating permission: {Name}", permission.Name);
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        [HttpDelete("delete-permission/{permissionId}")]
        public async Task<IActionResult> DeletePermission(Guid permissionId)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("User {UserId} attempting to delete permission: {PermissionId}", userId, permissionId);

                var result = await _menuRepository.DeletePermissionAsync(userId, permissionId);
                if (!result.Success)
                {
                    if (result.Message.Contains("Access denied"))
                    {
                        _logger.LogWarning("Unauthorized permission deletion attempt: {Message}", result.Message);
                        return Unauthorized(result);
                    }

                    _logger.LogWarning("Failed to delete permission: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Permission deleted successfully: {PermissionId}", permissionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting permission {PermissionId}.", permissionId);
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        [HttpDelete("delete-menu-permission")]
        public async Task<IActionResult> DeleteMenuPermissions([FromBody] DeleteMenuPermissionDTO dto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("User {UserId} attempting to delete multiple menu permissions for Menu {MenuId}", userId, dto.MenuId);

                var result = await _menuRepository.DeleteMenuPermissionsAsync(userId, dto.MenuId, dto.PermissionIds);

                if (!result.Success)
                {
                    if (result.Message.Contains("Access denied"))
                    {
                        _logger.LogWarning("Unauthorized attempt: {Message}", result.Message);
                        return Unauthorized(result);
                    }

                    _logger.LogWarning("Deletion failed: {Message}", result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Menu permissions deleted successfully for Menu {MenuId}", dto.MenuId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting menu permissions.");
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
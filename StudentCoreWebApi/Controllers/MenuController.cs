using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Response;

namespace StudentCoreWebApi.Controllers
{
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

        [HttpPost("create")]
        public async Task<IActionResult> CreateMenu([FromBody] MenuDTO menuDto)
        {
            try
            {
                var result = await _menuRepository.CreateMenuAsync(menuDto);
                if (result)
                {
                    _logger.LogInformation("Menu created successfully: {Title}", menuDto.Title);
                    var response = new ApiResponse<object>(true, "Menu created successfully!");
                    return Ok(response);
                }

                _logger.LogWarning("Failed to create menu: {Title}", menuDto.Title);
                return BadRequest(new ApiResponse<object>(false, "Failed to create menu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating menu.");
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        [HttpPost("assign-permission")]
        public async Task<IActionResult> AssignPermissionToMenu([FromBody] AssignPermissionDto dto)
        {
            try
            {
                await _menuRepository.AssignPermissionToMenuAsync(dto.MenuId, dto.PermissionIds);

                var response = new ApiResponse<object>(
                    true,
                    "Permission assigned successfully.",
                    new { menuId = dto.MenuId, permissionIds = dto.PermissionIds }
                );

                _logger.LogInformation("Permissions assigned to Menu {MenuId}", dto.MenuId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("AssignPermissionToMenu failed: {Message}", ex.Message);
                var errorResponse = new ApiResponse<object>(false, ex.Message);
                return BadRequest(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while assigning permissions to Menu {MenuId}", dto.MenuId);
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }
    }
}

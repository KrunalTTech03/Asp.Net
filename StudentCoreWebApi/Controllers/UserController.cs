using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Response;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace UserCoreWebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _UserRepository;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserRepository UserRepository, ILogger<UserController> logger)
        {
            _UserRepository = UserRepository;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string query = "",
            [FromQuery] string sortBy = "FirstName",
            [FromQuery] string sortOrder = "asc",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("SearchUsers called with query: {Query}, sortBy: {SortBy}, sortOrder: {SortOrder}, pageNumber: {PageNumber}, pageSize: {PageSize}",
                                    query, sortBy, sortOrder, pageNumber, pageSize);

            var response = await _UserRepository.GetAllUsersAsync(query, sortBy, sortOrder, pageNumber, pageSize);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("SearchUsers no Users found or failed: {Message}", response?.Message ?? "No Users found");
                return NotFound(response);
            }

            _logger.LogInformation("SearchUsers completed successfully.");
            return Ok(response);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUser addUser)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddUser failed due to invalid model state.");
                return BadRequest(ModelState);
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            try
            {
                var response = await _UserRepository.AddUserAsync(addUser, role);

                if (!response.Success)
                {
                    _logger.LogError("AddUser failed: {Message}", response.Message);
                    return BadRequest(response);
                }

                _logger.LogInformation("User added successfully with ID: {UserId}", response.Data?.Id);
                return CreatedAtAction(nameof(GetUser), new { id = response.Data?.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving User data.");
                return StatusCode(500, new ApiResponse<string>(false, ex.Message));
            }
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("UpsertUser")]
        public async Task<IActionResult> AddOrEditUser(Guid? id, [FromBody] UpsertDto userDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddOrEditUser: Invalid model state.");
                return BadRequest(ModelState);
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var response = await _UserRepository.AddOrEditUserAsync(id, userDto, role);

            if (!response.Success)
            {
                _logger.LogError("AddOrEditUser: Operation failed. {Message}", response.Message);
                return BadRequest(response);
            }

            _logger.LogInformation("AddOrEditUser: Operation succeeded.");
            return Ok(response);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUser([FromRoute] Guid id)
        {
            _logger.LogInformation("GetUser called for User ID: {UserId}", id);

            var response = await _UserRepository.GetByIdAsync(id);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("GetUser not found for ID: {UserId}", id);
                return NotFound(response);
            }

            _logger.LogInformation("GetUser found User with ID: {UserId}", id);
            return Ok(response);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUser updateUser)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateUser failed due to invalid model state.");
                return BadRequest(ModelState);
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation("UpdateUser called for User ID: {UserId}", updateUser.Id);

            var response = await _UserRepository.UpdateAsync(updateUser.Id, updateUser, role);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("UpdateUser failed for ID: {UserId}", updateUser.Id);
                return NotFound(response);
            }

            _logger.LogInformation("UpdateUser successfully updated User with ID: {UserId}", updateUser.Id);
            return Ok(response);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
        {

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation("DeleteUser called for User ID: {UserId}", id);

            var response = await _UserRepository.DeleteAsync(id, role);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("DeleteUser failed for ID: {UserId}", id);
                return NotFound(response);
            }

            _logger.LogInformation("DeleteUser successfully deleted User with ID: {UserId}", id);
            return Ok(response);
        }
    }
}
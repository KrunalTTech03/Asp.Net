using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.DTOs;
using StudentCoreWebApi.Interface;
using StudentCoreWebApi.Response;
using StudentCoreWebApi.Services;
using System.Threading.Tasks;

namespace StudentCoreWebApi.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _UserRepository;
        private readonly JwtTokenService _jwtTokenService;
        private readonly PasswordService _passwordService;
        private readonly ILogger<AuthController> _logger;
        private readonly IPermissionRepository _permissionRepository;

        public AuthController(
            IUserRepository UserRepository,
            JwtTokenService jwtTokenService,
            PasswordService passwordService,
            ILogger<AuthController> logger,
            IPermissionRepository permissionRepository)
        {
            _UserRepository = UserRepository;
            _jwtTokenService = jwtTokenService;
            _passwordService = passwordService;
            _logger = logger;
            _permissionRepository = permissionRepository;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User?.Identity?.Name;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new ApiResponse<string>(false, "Invalid or missing user identity"));
            }

            var response = await _UserRepository.GetCurrentUserProfileAsync(userId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var existingUser = await _UserRepository.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<string>(false, "Email already registered"));
                }
                var response = await _UserRepository.RegisterUserAsync(request);
                if (!response.Success)
                {
                    _logger.LogError("User registration failed: {Message}", response.Message);
                    return BadRequest(response);
                }
                _logger.LogInformation("User registered successfully with Email: {Email}", request.Email);
                return Ok(response);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error occurred while registering user");
                return StatusCode(500, new ApiResponse<string>(false, ex.InnerException?.Message ?? ex.Message));
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _UserRepository.LoginUserAsync(request, _jwtTokenService);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(response);
        }

        [HttpPost("mimic-login")]
        public async Task<IActionResult> MimicLogin([FromBody] MimicLoginRequest request)
        {
            var currentUserId = User?.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new ApiResponse<string>(false, "Current user not authenticated"));
            }

            var result = await _UserRepository.MimicLoginAsync(Guid.Parse(currentUserId), request.Email, _jwtTokenService);

            if (!result.Success)
            {
                if (result.Message.Contains("not authenticated") || result.Message.Contains("Only admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<string>(false, result.Message));
                }

                if (result.Message.Contains("not found") || result.Message.Contains("not assigned"))
                {
                    return NotFound(result);
                }

                return BadRequest(result);
            }

            _logger.LogInformation("Admin user {AdminId} mimic login for {Email}", currentUserId, request.Email);
            return Ok(result);
        }


    }
}
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

        public AuthController(IUserRepository UserRepository, JwtTokenService jwtTokenService, PasswordService passwordService, ILogger<AuthController> logger)
        {
            _UserRepository = UserRepository;
            _jwtTokenService = jwtTokenService;
            _passwordService = passwordService;
            _logger = logger;
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

    }
}

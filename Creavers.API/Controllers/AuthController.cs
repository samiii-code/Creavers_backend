using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Auth;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Authentication endpoints for registration and login.</summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IValidator<RegisterRequest> registerValidator,
            IValidator<LoginRequest> loginValidator,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _logger = logger;
        }

        /// <summary>Register a new user with a specified role (ADMIN, PROVIDER, CUSTOMER).</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            try
            {
                var result = await _authService.RegisterAsync(request, cancellationToken);
                return StatusCode(StatusCodes.Status201Created,
                    ApiResponse<AuthResponse>.SuccessResult(result, "User registered successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Login using email or phone number and password.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            try
            {
                var result = await _authService.LoginAsync(request, cancellationToken);
                return Ok(ApiResponse<AuthResponse>.SuccessResult(result, "Login successful."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.FailureResult(ex.Message));
            }
        }
    }
}

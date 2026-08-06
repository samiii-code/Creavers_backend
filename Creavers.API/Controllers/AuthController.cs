using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Auth;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Authentication endpoints for registration, login, and OTP verification.</summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;
        private readonly IWebHostEnvironment _environment;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<SendOtpRequest> _sendOtpValidator;
        private readonly IValidator<VerifyOtpRequest> _verifyOtpValidator;
        private readonly IValidator<ResendOtpRequest> _resendOtpValidator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IOtpService otpService,
            IWebHostEnvironment environment,
            IValidator<RegisterRequest> registerValidator,
            IValidator<LoginRequest> loginValidator,
            IValidator<SendOtpRequest> sendOtpValidator,
            IValidator<VerifyOtpRequest> verifyOtpValidator,
            IValidator<ResendOtpRequest> resendOtpValidator,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _otpService = otpService;
            _environment = environment;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _sendOtpValidator = sendOtpValidator;
            _verifyOtpValidator = verifyOtpValidator;
            _resendOtpValidator = resendOtpValidator;
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

        /// <summary>Send an OTP code to a user for phone verification, email verification, or password reset.</summary>
        [HttpPost("send-otp")]
        [ProducesResponseType(typeof(ApiResponse<OtpResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
        {
            var validation = await _sendOtpValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            try
            {
                var result = await _otpService.SendOtpAsync(request, _environment.IsDevelopment(), cancellationToken);
                return Ok(ApiResponse<OtpResponse>.SuccessResult(result, result.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Verify an OTP code submitted by a user.</summary>
        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(ApiResponse<OtpResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
        {
            var validation = await _verifyOtpValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            try
            {
                var result = await _otpService.VerifyOtpAsync(request, cancellationToken);
                return Ok(ApiResponse<OtpResponse>.SuccessResult(result, result.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Resend an OTP code to a user.</summary>
        [HttpPost("resend-otp")]
        [ProducesResponseType(typeof(ApiResponse<OtpResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken cancellationToken)
        {
            var validation = await _resendOtpValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            try
            {
                var result = await _otpService.ResendOtpAsync(request, _environment.IsDevelopment(), cancellationToken);
                return Ok(ApiResponse<OtpResponse>.SuccessResult(result, result.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
        }
    }
}

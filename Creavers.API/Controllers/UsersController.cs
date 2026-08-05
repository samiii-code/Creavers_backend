using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Users;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>User management endpoints.</summary>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Get all registered users. ADMIN only.</summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllUsersAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResult(users));
        }

        /// <summary>Get a user by ID. Requires authentication.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (user == null)
                return NotFound(ApiResponse<object>.FailureResult($"User '{id}' not found."));

            return Ok(ApiResponse<UserDto>.SuccessResult(user));
        }
    }
}

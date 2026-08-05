using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Providers;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Admin endpoints for managing provider approvals.</summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Get all provider profiles. ADMIN only.</summary>
        [HttpGet("providers")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProviderProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllProviders(CancellationToken cancellationToken)
        {
            var providers = await _adminService.GetAllProvidersAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<ProviderProfileDto>>.SuccessResult(providers));
        }

        /// <summary>Get all pending provider profiles awaiting approval. ADMIN only.</summary>
        [HttpGet("providers/pending")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProviderProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingProviders(CancellationToken cancellationToken)
        {
            var providers = await _adminService.GetPendingProvidersAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<ProviderProfileDto>>.SuccessResult(providers));
        }

        /// <summary>Approve a provider profile. Sets status to Approved. ADMIN only.</summary>
        [HttpPatch("providers/{id:guid}/approve")]
        [ProducesResponseType(typeof(ApiResponse<ProviderProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveProvider(Guid id, CancellationToken cancellationToken)
        {
            var profile = await _adminService.ApproveProviderAsync(id, cancellationToken);
            if (profile == null)
                return NotFound(ApiResponse<object>.FailureResult($"Provider profile '{id}' not found."));

            return Ok(ApiResponse<ProviderProfileDto>.SuccessResult(profile, "Provider approved successfully."));
        }

        /// <summary>Reject a provider profile. Sets status to Rejected. ADMIN only.</summary>
        [HttpPatch("providers/{id:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<ProviderProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectProvider(Guid id, CancellationToken cancellationToken)
        {
            var profile = await _adminService.RejectProviderAsync(id, cancellationToken);
            if (profile == null)
                return NotFound(ApiResponse<object>.FailureResult($"Provider profile '{id}' not found."));

            return Ok(ApiResponse<ProviderProfileDto>.SuccessResult(profile, "Provider rejected successfully."));
        }
    }
}

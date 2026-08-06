using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Tasks;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Customer task management endpoints.</summary>
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    [Produces("application/json")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IValidator<CreateTaskRequest> _createValidator;
        private readonly IValidator<UpdateTaskRequest> _updateValidator;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskService taskService,
            IValidator<CreateTaskRequest> createValidator,
            IValidator<UpdateTaskRequest> updateValidator,
            IWebHostEnvironment environment,
            ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>Create a new customer task. CUSTOMER role only. Optional image file attachment supported via multipart form data.</summary>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [Consumes("application/json", "multipart/form-data")]
        public async Task<IActionResult> CreateTask(
            [FromForm] CreateTaskRequest request,
            IFormFile? image,
            CancellationToken cancellationToken)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            var customerId = GetCurrentUserId();
            string? imagePath = null;

            if (image != null && image.Length > 0)
            {
                imagePath = await SaveImageFileAsync(image, cancellationToken);
            }

            try
            {
                var task = await _taskService.CreateAsync(request, customerId, imagePath, cancellationToken);
                return StatusCode(StatusCodes.Status201Created,
                    ApiResponse<TaskResponse>.SuccessResult(task, "Task created successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Get all tasks. ADMIN role only.</summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TaskResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTasks(CancellationToken cancellationToken)
        {
            var tasks = await _taskService.GetAllAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResult(tasks));
        }

        /// <summary>Get all tasks owned by the authenticated customer. CUSTOMER role only.</summary>
        [HttpGet("my")]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TaskResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyTasks(CancellationToken cancellationToken)
        {
            var customerId = GetCurrentUserId();
            var tasks = await _taskService.GetMyTasksAsync(customerId, cancellationToken);
            return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResult(tasks));
        }

        /// <summary>Get a task by ID. CUSTOMER and ADMIN roles.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTaskById(Guid id, CancellationToken cancellationToken)
        {
            var task = await _taskService.GetByIdAsync(id, cancellationToken);
            if (task == null)
                return NotFound(ApiResponse<object>.FailureResult($"Task '{id}' not found."));

            // If user is CUSTOMER, ensure ownership
            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (task.CustomerId != userId)
                    return Forbid();
            }

            return Ok(ApiResponse<TaskResponse>.SuccessResult(task));
        }

        /// <summary>Update an existing task. CUSTOMER (own tasks) and ADMIN roles.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTask(
            Guid id,
            [FromBody] UpdateTaskRequest request,
            CancellationToken cancellationToken)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            var requesterId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            try
            {
                var updated = await _taskService.UpdateAsync(id, request, requesterId, isAdmin, cancellationToken);
                return Ok(ApiResponse<TaskResponse>.SuccessResult(updated, "Task updated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Delete a task (soft delete). CUSTOMER (own tasks) and ADMIN roles.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
        {
            var requesterId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            try
            {
                await _taskService.DeleteAsync(id, requesterId, isAdmin, cancellationToken);
                return Ok(ApiResponse<object?>.SuccessResult(null, "Task deleted successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException("User ID claim missing.");
            return Guid.Parse(claim);
        }

        private bool IsAdmin()
        {
            return User.IsInRole("ADMIN");
        }

        private async Task<string> SaveImageFileAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "tasks");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return Path.Combine("uploads", "tasks", uniqueFileName).Replace("\\", "/");
        }
    }
}

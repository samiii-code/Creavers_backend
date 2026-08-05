using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Categories;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Category management endpoints.</summary>
    [ApiController]
    [Route("api/categories")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IValidator<CreateCategoryRequest> _createValidator;
        private readonly IValidator<UpdateCategoryRequest> _updateValidator;

        public CategoriesController(
            ICategoryService categoryService,
            IValidator<CreateCategoryRequest> createValidator,
            IValidator<UpdateCategoryRequest> updateValidator)
        {
            _categoryService = categoryService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        /// <summary>Get all categories. Public access.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var categories = await _categoryService.GetAllAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<CategoryDto>>.SuccessResult(categories));
        }

        /// <summary>Get a category by ID. Public access.</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var category = await _categoryService.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return NotFound(ApiResponse<object>.FailureResult($"Category '{id}' not found."));

            return Ok(ApiResponse<CategoryDto>.SuccessResult(category));
        }

        /// <summary>Create a new category. ADMIN only.</summary>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            var category = await _categoryService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<CategoryDto>.SuccessResult(category, "Category created successfully."));
        }

        /// <summary>Update a category. ADMIN only.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            var category = await _categoryService.UpdateAsync(id, request, cancellationToken);
            if (category == null)
                return NotFound(ApiResponse<object>.FailureResult($"Category '{id}' not found."));

            return Ok(ApiResponse<CategoryDto>.SuccessResult(category, "Category updated successfully."));
        }

        /// <summary>Delete a category (soft delete). ADMIN only.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _categoryService.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(ApiResponse<object>.FailureResult($"Category '{id}' not found."));

            return Ok(ApiResponse<object>.SuccessResult(null!, "Category deleted successfully."));
        }
    }
}

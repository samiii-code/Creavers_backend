using Creavers.API.DTOs.Categories;

namespace Creavers.API.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
        Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

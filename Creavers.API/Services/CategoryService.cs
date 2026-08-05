using AutoMapper;
using Creavers.API.DTOs.Categories;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IRepository<Category> repository, IMapper mapper, ILogger<CategoryService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _repository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var category = await _repository.GetByIdAsync(id, cancellationToken);
            return category == null ? null : _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var category = _mapper.Map<Category>(request);
            await _repository.AddAsync(category, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category created: {Name}", category.Name);
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var category = await _repository.GetByIdAsync(id, cancellationToken);
            if (category == null) return null;

            _mapper.Map(request, category);
            _repository.Update(category);
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category updated: {Id}", id);
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var category = await _repository.GetByIdAsync(id, cancellationToken);
            if (category == null) return false;

            _repository.Delete(category);
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Category soft-deleted: {Id}", id);
            return true;
        }
    }
}

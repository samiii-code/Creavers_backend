using Creavers.API.DTOs.Tasks;

namespace Creavers.API.Interfaces
{
    public interface ITaskService
    {
        Task<TaskResponse>              CreateAsync(CreateTaskRequest request, Guid customerId, string? imagePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<TaskResponse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<TaskResponse?>             GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TaskResponse>              UpdateAsync(Guid id, UpdateTaskRequest request, Guid requesterId, bool isAdmin, CancellationToken cancellationToken = default);
        Task                            DeleteAsync(Guid id, Guid requesterId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<IEnumerable<TaskResponse>> GetMyTasksAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<RecommendedProviderDto>> GetRecommendedProvidersAsync(Guid taskId, CancellationToken cancellationToken = default);
    }
}

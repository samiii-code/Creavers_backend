using Creavers.API.DTOs.Users;

namespace Creavers.API.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

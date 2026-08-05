using Microsoft.AspNetCore.Identity;
using Creavers.API.DTOs.Users;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(MapToDto(user, roles.FirstOrDefault() ?? string.Empty));
            }

            return result;
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return MapToDto(user, roles.FirstOrDefault() ?? string.Empty);
        }

        private static UserDto MapToDto(ApplicationUser user, string role) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = role,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt
        };
    }
}

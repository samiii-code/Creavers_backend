using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Creavers.API.DTOs.Auth;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IMapper mapper,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            // Check duplicate email
            var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingByEmail != null)
                throw new InvalidOperationException("A user with this email already exists.");

            // Check duplicate phone
            var existingByPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber);
            if (existingByPhone != null)
                throw new InvalidOperationException("A user with this phone number already exists.");

            var user = _mapper.Map<ApplicationUser>(request);
            user.Id = Guid.NewGuid();
            user.UserName = request.Email;
            user.SecurityStamp = Guid.NewGuid().ToString();

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"User creation failed: {errors}");
            }

            var role = request.Role.ToUpper();
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Role assignment failed: {errors}");
            }

            _logger.LogInformation("New user registered: {Email} with role {Role}", user.Email, role);

            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, role);

            return new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = role,
                ExpiresAt = expiresAt
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            // Resolve user by email OR phone
            ApplicationUser? user = null;

            if (request.EmailOrPhone.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(request.EmailOrPhone);
            }
            else
            {
                user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == request.EmailOrPhone);
            }

            if (user == null)
                throw new UnauthorizedAccessException("Invalid credentials.");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Invalid credentials.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "CUSTOMER";

            _logger.LogInformation("User logged in: {Email}", user.Email);

            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, role);

            return new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = role,
                ExpiresAt = expiresAt
            };
        }
    }
}

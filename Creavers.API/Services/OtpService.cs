using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Data;
using Creavers.API.DTOs.Auth;
using Creavers.API.Interfaces;
using Creavers.API.Models;
using Creavers.API.Models.Enums;

namespace Creavers.API.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext          _context;
        private readonly UserManager<ApplicationUser>  _userManager;
        private readonly ILogger<OtpService>           _logger;

        private const int OtpExpiryMinutes = 5;

        public OtpService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<OtpService> logger)
        {
            _context     = context;
            _userManager = userManager;
            _logger      = logger;
        }

        // ────────────────────────────────────────────────────────────────────
        public async Task<OtpResponse> SendOtpAsync(
            SendOtpRequest request,
            bool isDevelopment,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new KeyNotFoundException($"User '{request.UserId}' not found.");

            var code = await GenerateAndSaveOtpAsync(user.Id, request.Purpose, cancellationToken);

            // In development: log and return the code; in production log only
            _logger.LogInformation(
                "[OTP] UserId={UserId} Purpose={Purpose} Code={Code} ExpiresIn={Minutes}min",
                user.Id, request.Purpose, code, OtpExpiryMinutes);

            return new OtpResponse
            {
                Message = $"OTP sent successfully. It expires in {OtpExpiryMinutes} minutes.",
                OtpCode = isDevelopment ? code : null
            };
        }

        // ────────────────────────────────────────────────────────────────────
        public async Task<OtpResponse> VerifyOtpAsync(
            VerifyOtpRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new KeyNotFoundException($"User '{request.UserId}' not found.");

            // Find the latest matching, unused, non-expired OTP
            var otp = await _context.OtpCodes
                .Where(o =>
                    o.UserId    == request.UserId &&
                    o.Code      == request.Code   &&
                    o.Purpose   == request.Purpose &&
                    !o.IsUsed   &&
                    !o.IsDeleted &&
                    o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp == null)
                throw new InvalidOperationException("Invalid or expired OTP code.");

            // Mark as used
            otp.IsUsed    = true;
            otp.UpdatedAt = DateTime.UtcNow;

            // Mark user as verified for phone / email verification
            if (request.Purpose is OtpPurpose.PhoneVerification or OtpPurpose.EmailVerification)
            {
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[OTP] Verified — UserId={UserId} Purpose={Purpose}",
                request.UserId, request.Purpose);

            return new OtpResponse { Message = "OTP verified successfully." };
        }

        // ────────────────────────────────────────────────────────────────────
        public async Task<OtpResponse> ResendOtpAsync(
            ResendOtpRequest request,
            bool isDevelopment,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new KeyNotFoundException($"User '{request.UserId}' not found.");

            // Invalidate all previous active OTPs for this user + purpose
            var previous = await _context.OtpCodes
                .Where(o =>
                    o.UserId  == request.UserId &&
                    o.Purpose == request.Purpose &&
                    !o.IsUsed &&
                    !o.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var old in previous)
            {
                old.IsUsed    = true;
                old.UpdatedAt = DateTime.UtcNow;
            }

            var code = await GenerateAndSaveOtpAsync(user.Id, request.Purpose, cancellationToken);

            _logger.LogInformation(
                "[OTP-RESEND] UserId={UserId} Purpose={Purpose} Code={Code}",
                user.Id, request.Purpose, code);

            return new OtpResponse
            {
                Message = $"OTP resent successfully. It expires in {OtpExpiryMinutes} minutes.",
                OtpCode = isDevelopment ? code : null
            };
        }

        // ────────────────────────────────────────────────────────────────────
        private async Task<string> GenerateAndSaveOtpAsync(
            Guid userId,
            OtpPurpose purpose,
            CancellationToken cancellationToken)
        {
            var code = Random.Shared.Next(100_000, 999_999).ToString();

            var otp = new OtpCode
            {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Code      = code,
                Purpose   = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                IsUsed    = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.OtpCodes.AddAsync(otp, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return code;
        }
    }
}

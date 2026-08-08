using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Creavers.API.Data;
using Creavers.API.DTOs.Jobs;
using Creavers.API.Interfaces;
using Creavers.API.Models;
using Creavers.API.Models.Enums;

namespace Creavers.API.Services
{
    public class JobService : IJobService
    {
        private readonly ApplicationDbContext  _context;
        private readonly INotificationService  _notificationService;
        private readonly IAuditLogService      _auditLog;
        private readonly IMapper               _mapper;
        private readonly ILogger<JobService>   _logger;
        private readonly IWebHostEnvironment   _env;

        public JobService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditLogService auditLog,
            IMapper mapper,
            ILogger<JobService> logger,
            IWebHostEnvironment env)
        {
            _context             = context;
            _notificationService = notificationService;
            _auditLog            = auditLog;
            _mapper              = mapper;
            _logger              = logger;
            _env                 = env;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<Booking> GetBookingForProviderAsync(
            Guid bookingId, Guid providerUserId, CancellationToken ct)
        {
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .Include(b => b.Task)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            if (booking.Provider.ApplicationUserId != providerUserId)
                throw new UnauthorizedAccessException("Only the assigned provider can update job status.");

            if (booking.BookingStatus != BookingStatus.Accepted)
                throw new InvalidOperationException("Job can only be managed on an Accepted booking.");

            return booking;
        }

        private async Task AddTimelineEntryAsync(
            Guid bookingId, JobStatus status, Guid changedBy,
            string? notes, CancellationToken ct)
        {
            var entry = new JobTimeline
            {
                BookingId = bookingId,
                Status    = status,
                ChangedBy = changedBy,
                Notes     = notes,
                CreatedAt = DateTime.UtcNow
            };
            _context.JobTimelines.Add(entry);
            await _context.SaveChangesAsync(ct);
        }

        private static JobStatusResponse BuildResponse(Booking booking, string? msg = null) =>
            new()
            {
                BookingId     = booking.Id,
                JobStatus     = booking.JobStatus,
                UpdatedAt     = booking.UpdatedAt ?? booking.CreatedAt,
                Message       = msg
            };

        // ─────────────────────────────────────────────────────────────────────────
        // 1. EN-ROUTE
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<JobStatusResponse> SetEnRouteAsync(
            Guid bookingId, Guid providerUserId, CancellationToken ct = default)
        {
            var booking = await GetBookingForProviderAsync(bookingId, providerUserId, ct);

            if (booking.JobStatus != JobStatus.Accepted)
                throw new InvalidOperationException(
                    $"Cannot move to EnRoute from {booking.JobStatus}. Expected: Accepted.");

            booking.JobStatus  = JobStatus.EnRoute;
            booking.UpdatedAt  = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await AddTimelineEntryAsync(bookingId, JobStatus.EnRoute, providerUserId,
                "Provider is now en route.", ct);

            await _notificationService.CreateNotificationAsync(
                booking.CustomerId,
                "Provider En Route",
                $"Your provider is on the way for task '{booking.Task?.Title}'.",
                ct);

            await _auditLog.LogAsync(providerUserId, "JobEnRoute", "Booking",
                bookingId.ToString(), null, ct);

            _logger.LogInformation("Booking {BookingId} moved to EnRoute by provider {UserId}",
                bookingId, providerUserId);

            return BuildResponse(booking, "Provider is now en route.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 2. ARRIVED
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<JobStatusResponse> SetArrivedAsync(
            Guid bookingId, Guid providerUserId, CancellationToken ct = default)
        {
            var booking = await GetBookingForProviderAsync(bookingId, providerUserId, ct);

            if (booking.JobStatus != JobStatus.EnRoute)
                throw new InvalidOperationException(
                    $"Cannot move to Arrived from {booking.JobStatus}. Expected: EnRoute.");

            booking.JobStatus = JobStatus.Arrived;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await AddTimelineEntryAsync(bookingId, JobStatus.Arrived, providerUserId,
                "Provider has arrived.", ct);

            await _notificationService.CreateNotificationAsync(
                booking.CustomerId,
                "Provider Arrived",
                $"Your provider has arrived for task '{booking.Task?.Title}'.",
                ct);

            await _auditLog.LogAsync(providerUserId, "JobArrived", "Booking",
                bookingId.ToString(), null, ct);

            _logger.LogInformation("Booking {BookingId} moved to Arrived by provider {UserId}",
                bookingId, providerUserId);

            return BuildResponse(booking, "Provider has arrived.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 3. REQUEST START OTP (customer calls this)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<RequestStartOtpResponse> RequestStartOtpAsync(
            Guid bookingId, Guid customerUserId, CancellationToken ct = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .Include(b => b.Task)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            if (booking.CustomerId != customerUserId)
                throw new UnauthorizedAccessException("Only the booking customer can request a start OTP.");

            if (booking.JobStatus != JobStatus.Arrived)
                throw new InvalidOperationException(
                    $"OTP can only be requested when provider has Arrived. Current: {booking.JobStatus}.");

            // Invalidate any existing active OTPs for this booking+purpose
            var existing = await _context.OtpCodes
                .Where(o => o.UserId == customerUserId
                         && o.Purpose == OtpPurpose.JobStart
                         && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);

            foreach (var old in existing)
                old.IsUsed = true;

            // Generate new OTP
            var otpCode = new Random().Next(100000, 999999).ToString();
            var otp = new OtpCode
            {
                UserId    = customerUserId,
                Code      = otpCode,
                Purpose   = OtpPurpose.JobStart,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed    = false
            };
            _context.OtpCodes.Add(otp);

            // Move booking to StartPendingOTP
            booking.JobStatus = JobStatus.StartPendingOTP;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await AddTimelineEntryAsync(bookingId, JobStatus.StartPendingOTP, customerUserId,
                "Customer requested job-start OTP.", ct);

            await _notificationService.CreateNotificationAsync(
                booking.Provider.ApplicationUserId,
                "OTP Generated",
                $"Customer has generated a start OTP for task '{booking.Task?.Title}'. Ask customer for the code.",
                ct);

            await _auditLog.LogAsync(customerUserId, "JobOtpRequested", "Booking",
                bookingId.ToString(), null, ct);

            _logger.LogInformation(
                "Job Start OTP for Booking {BookingId}: {Otp} (expires {ExpiresAt})",
                bookingId, otpCode, otp.ExpiresAt);

            bool isDev = _env.IsDevelopment();
            return new RequestStartOtpResponse
            {
                BookingId = bookingId,
                ExpiresAt = otp.ExpiresAt,
                Otp       = isDev ? otpCode : null,   // expose OTP only in development
                Message   = isDev
                    ? $"[DEV] OTP is {otpCode}. Expires at {otp.ExpiresAt:HH:mm:ss} UTC."
                    : "OTP has been generated. Share it with your provider to start the job."
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 4. VERIFY START OTP (provider calls this)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<JobStatusResponse> VerifyStartOtpAsync(
            Guid bookingId, Guid providerUserId, string otp, CancellationToken ct = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .Include(b => b.Task)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            if (booking.Provider.ApplicationUserId != providerUserId)
                throw new UnauthorizedAccessException("Only the assigned provider can verify the OTP.");

            if (booking.JobStatus != JobStatus.StartPendingOTP)
                throw new InvalidOperationException(
                    $"No OTP verification pending. Current status: {booking.JobStatus}.");

            // Find valid OTP for the booking's customer
            var otpRecord = await _context.OtpCodes
                .Where(o => o.UserId    == booking.CustomerId
                         && o.Purpose   == OtpPurpose.JobStart
                         && o.Code      == otp.Trim()
                         && !o.IsUsed
                         && o.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync(ct);

            if (otpRecord == null)
                throw new InvalidOperationException("Invalid or expired OTP.");

            // Consume OTP
            otpRecord.IsUsed  = true;
            otpRecord.UpdatedAt = DateTime.UtcNow;

            booking.JobStatus = JobStatus.InProgress;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await AddTimelineEntryAsync(bookingId, JobStatus.InProgress, providerUserId,
                "Job started — OTP verified by provider.", ct);

            await _notificationService.CreateNotificationAsync(
                booking.CustomerId,
                "Job Started",
                $"Your job for task '{booking.Task?.Title}' has started.",
                ct);

            await _auditLog.LogAsync(providerUserId, "JobStarted", "Booking",
                bookingId.ToString(), "OTP verified successfully.", ct);

            _logger.LogInformation("Booking {BookingId} started (OTP verified) by provider {UserId}",
                bookingId, providerUserId);

            return BuildResponse(booking, "OTP verified. Job is now InProgress.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 5. COMPLETE JOB
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<JobStatusResponse> CompleteJobAsync(
            Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .Include(b => b.Task)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            if (!isAdmin
                && booking.Provider.ApplicationUserId != userId
                && booking.CustomerId != userId)
                throw new UnauthorizedAccessException("Only the booking participants or an admin can complete the job.");

            if (booking.JobStatus != JobStatus.InProgress)
                throw new InvalidOperationException(
                    $"Job must be InProgress before completing. Current: {booking.JobStatus}.");

            booking.JobStatus    = JobStatus.Completed;
            booking.BookingStatus = BookingStatus.Completed;
            booking.UpdatedAt    = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            await AddTimelineEntryAsync(bookingId, JobStatus.Completed, userId,
                "Job marked as completed.", ct);

            // Notify counterparty
            var recipientId = userId == booking.CustomerId
                ? booking.Provider.ApplicationUserId
                : booking.CustomerId;

            await _notificationService.CreateNotificationAsync(
                recipientId,
                "Job Completed",
                $"The job for task '{booking.Task?.Title}' has been completed.",
                ct);

            await _auditLog.LogAsync(userId, "JobCompleted", "Booking",
                bookingId.ToString(), null, ct);

            _logger.LogInformation("Booking {BookingId} completed by user {UserId}", bookingId, userId);

            return BuildResponse(booking, "Job has been completed.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 6. UPLOAD COMPLETION EVIDENCE
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<CompletionEvidenceResponse> UploadEvidenceAsync(
            Guid bookingId, Guid providerUserId,
            IFormFile photo, string? description, CancellationToken ct = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .Include(b => b.Task)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            if (booking.Provider.ApplicationUserId != providerUserId)
                throw new UnauthorizedAccessException("Only the assigned provider can upload completion evidence.");

            if (booking.JobStatus != JobStatus.InProgress && booking.JobStatus != JobStatus.Completed)
                throw new InvalidOperationException(
                    "Evidence can only be uploaded when job is InProgress or Completed.");

            // Validate file
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                throw new InvalidOperationException("Only JPEG, PNG, and WEBP images are allowed.");

            if (photo.Length > 10 * 1024 * 1024)
                throw new InvalidOperationException("Photo must not exceed 10 MB.");

            // Save file locally
            var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "evidence", bookingId.ToString());
            Directory.CreateDirectory(uploadDir);

            var ext      = Path.GetExtension(photo.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            await using (var stream = File.Create(filePath))
                await photo.CopyToAsync(stream, ct);

            // Store relative path
            var relativePath = Path.Combine("uploads", "evidence", bookingId.ToString(), fileName)
                                   .Replace('\\', '/');

            var evidence = new CompletionEvidence
            {
                BookingId   = bookingId,
                PhotoPath   = relativePath,
                Description = description,
                UploadedAt  = DateTime.UtcNow,
                UploadedBy  = providerUserId
            };
            _context.CompletionEvidences.Add(evidence);
            await _context.SaveChangesAsync(ct);

            await _notificationService.CreateNotificationAsync(
                booking.CustomerId,
                "Completion Evidence Uploaded",
                $"Your provider has uploaded completion evidence for task '{booking.Task?.Title}'.",
                ct);

            await _auditLog.LogAsync(providerUserId, "EvidenceUploaded", "CompletionEvidence",
                evidence.Id.ToString(), $"File: {relativePath}", ct);

            _logger.LogInformation(
                "Evidence uploaded for Booking {BookingId}: {FilePath}", bookingId, relativePath);

            return _mapper.Map<CompletionEvidenceResponse>(evidence);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 7. GET TIMELINE
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<JobTimelineResponse>> GetTimelineAsync(
            Guid bookingId, Guid requestingUserId, CancellationToken ct = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            bool isParticipant = booking.CustomerId == requestingUserId
                              || booking.Provider.ApplicationUserId == requestingUserId;
            if (!isParticipant)
                throw new UnauthorizedAccessException("Only booking participants can view the timeline.");

            var entries = await _context.JobTimelines
                .Include(t => t.ChangedByUser)
                .Where(t => t.BookingId == bookingId && !t.IsDeleted)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<JobTimelineResponse>>(entries);
        }
    }
}

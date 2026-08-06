using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Models;
using Creavers.API.Models.Enums;

namespace Creavers.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category>        Categories     { get; set; } = null!;
        public DbSet<ProviderProfile> ProviderProfiles { get; set; } = null!;
        public DbSet<OtpCode>         OtpCodes       { get; set; } = null!;
        public DbSet<CustomerTask>    CustomerTasks  { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── ApplicationUser ──────────────────────────────────────────────
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // ── Category ─────────────────────────────────────────────────────
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.HasIndex(c => c.Name).IsUnique();
                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // ── ProviderProfile ───────────────────────────────────────────────
            modelBuilder.Entity<ProviderProfile>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Status)
                      .HasConversion<string>()
                      .HasDefaultValue(ProviderStatus.Pending);

                entity.Property(p => p.Bio).HasMaxLength(1000);
                entity.Property(p => p.ServiceArea).HasMaxLength(300);
                entity.Property(p => p.Availability).HasMaxLength(300);
                entity.Property(p => p.NationalId).IsRequired().HasMaxLength(50);

                // One User → One Profile
                entity.HasOne(p => p.ApplicationUser)
                      .WithOne(u => u.ProviderProfile)
                      .HasForeignKey<ProviderProfile>(p => p.ApplicationUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // One Category → Many Profiles
                entity.HasOne(p => p.Category)
                      .WithMany(c => c.ProviderProfiles)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.ApplicationUserId).IsUnique();
                entity.HasQueryFilter(p => !p.IsDeleted);
            });

            // ── OtpCode ───────────────────────────────────────────────────────
            modelBuilder.Entity<OtpCode>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Code).IsRequired().HasMaxLength(6);
                entity.Property(o => o.Purpose).HasConversion<string>();
                entity.Property(o => o.ExpiresAt).IsRequired();
                entity.Property(o => o.IsUsed).HasDefaultValue(false);
                entity.Property(o => o.CreatedAt).HasDefaultValueSql("NOW()");

                // One User → Many OtpCodes
                entity.HasOne(o => o.User)
                      .WithMany(u => u.OtpCodes)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(o => new { o.UserId, o.Purpose });
                entity.HasQueryFilter(o => !o.IsDeleted);
            });

            // ── CustomerTask ──────────────────────────────────────────────────
            modelBuilder.Entity<CustomerTask>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).IsRequired().HasMaxLength(2000);
                entity.Property(t => t.Address).IsRequired().HasMaxLength(500);
                entity.Property(t => t.SubCity).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Woreda).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Landmark).HasMaxLength(300);
                entity.Property(t => t.Budget).HasColumnType("decimal(18,2)");
                entity.Property(t => t.ImagePath).HasMaxLength(500);

                entity.Property(t => t.Status)
                      .HasConversion<string>()
                      .HasDefaultValue(CustomerTaskStatus.Pending);

                entity.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");

                // One Customer (User) → Many Tasks
                entity.HasOne(t => t.Customer)
                      .WithMany(u => u.CustomerTasks)
                      .HasForeignKey(t => t.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // One Category → Many Tasks
                entity.HasOne(t => t.Category)
                      .WithMany(c => c.CustomerTasks)
                      .HasForeignKey(t => t.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => t.CustomerId);
                entity.HasIndex(t => t.CategoryId);
                entity.HasQueryFilter(t => !t.IsDeleted);
            });
        }
    }
}

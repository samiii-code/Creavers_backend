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

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<ProviderProfile> ProviderProfiles { get; set; } = null!;

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
        }
    }
}

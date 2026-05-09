using FocusSpace.Domain.Entities;
using FocusSpace.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DomainTask = FocusSpace.Domain.Entities.Task;

namespace FocusSpace.Infrastructure.Data
{
    /// <summary>
    /// EF Core DbContext that integrates ASP.NET Core Identity (integer PKs).
    /// </summary>
    public class AppDbContext : IdentityDbContext<User, ApplicationRole, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DomainTask> Tasks => Set<DomainTask>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<Planet> Planets => Set<Planet>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IMPORTANT: must call base first so Identity tables are configured.
            base.OnModelCreating(modelBuilder);

            // ── User ──────────────────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Role)
                      .HasConversion<string>()
                      .HasDefaultValue(UserRole.User);

                entity.HasOne(u => u.CurrentPlanet)
                      .WithMany(p => p.Users)
                      .HasForeignKey(u => u.CurrentPlanetId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Planet ────────────────────────────────────────────────
            modelBuilder.Entity<Planet>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            });

            // ── Task ──────────────────────────────────────────────────
            modelBuilder.Entity<DomainTask>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(300);

                // Store priority as a human-readable string ("Low", "Medium", "High").
                entity.Property(t => t.Priority)
                      .HasConversion<string>()
                      .HasDefaultValue(TaskPriority.Medium);

                entity.HasOne(t => t.User)
                      .WithMany(u => u.Tasks)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Session ───────────────────────────────────────────────
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Status)
                      .HasConversion<string>()
                      .HasDefaultValue(SessionStatus.Ongoing);

                entity.HasOne(s => s.User)
                      .WithMany(u => u.Sessions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Task)
                      .WithMany(t => t.Sessions)
                      .HasForeignKey(s => s.TaskId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Seed Roles ────────────────────────────────────────────
            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = 1, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "static-admin-stamp" },
                new ApplicationRole { Id = 2, Name = "User",  NormalizedName = "USER",  ConcurrencyStamp = "static-user-stamp" }
            );

            // ── Seed Planets ──────────────────────────────────────────
            modelBuilder.Entity<Planet>().HasData(
                new Planet { Id = 1, Name = "Mercury", OrderNumber = 1, Description = "The closest planet to the Sun" },
                new Planet { Id = 2, Name = "Venus",   OrderNumber = 2, Description = "The hottest planet" },
                new Planet { Id = 3, Name = "Earth",   OrderNumber = 3, Description = "Our home planet" },
                new Planet { Id = 4, Name = "Mars",    OrderNumber = 4, Description = "The Red Planet" },
                new Planet { Id = 5, Name = "Jupiter", OrderNumber = 5, Description = "The largest planet" },
                new Planet { Id = 6, Name = "Saturn",  OrderNumber = 6, Description = "The ringed planet" },
                new Planet { Id = 7, Name = "Uranus",  OrderNumber = 7, Description = "The ice giant" },
                new Planet { Id = 8, Name = "Neptune", OrderNumber = 8, Description = "The farthest planet" }
            );
        }
    }
}
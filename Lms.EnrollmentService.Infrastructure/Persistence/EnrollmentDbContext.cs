using Lms.EnrollmentService.Application.Interfaces;
using Lms.EnrollmentService.Domain.Entities;
using Lms.EnrollmentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lms.EnrollmentService.Infrastructure.Persistence;

public class EnrollmentDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<WaitlistEntry> Waitlist => Set<WaitlistEntry>();

    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Enrollments");

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status)
                  .HasConversion(v => v.ToString(), v => Enum.Parse<EnrollmentStatus>(v))
                  .HasMaxLength(20);
            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
            entity.HasIndex(e => e.CourseId);
        });

        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.ToTable("Waitlist");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CourseId, e.Position });
        });
    }
}
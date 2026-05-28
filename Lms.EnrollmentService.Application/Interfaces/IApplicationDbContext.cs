using Lms.EnrollmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lms.EnrollmentService.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Enrollment> Enrollments { get; }
    DbSet<WaitlistEntry> Waitlist { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
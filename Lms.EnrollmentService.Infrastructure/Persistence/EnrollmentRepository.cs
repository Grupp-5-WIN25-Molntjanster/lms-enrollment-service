using Lms.EnrollmentService.Application.Common;
using Lms.EnrollmentService.Domain.Entities;
using Lms.EnrollmentService.Domain.Enums;
using Lms.EnrollmentService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lms.EnrollmentService.Infrastructure.Persistence;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly EnrollmentDbContext _context;
    public EnrollmentRepository(EnrollmentDbContext context) => _context = context;

    public async Task<Enrollment?> GetByIdAsync(Guid id) =>
        await _context.Enrollments.FindAsync(id);

    public async Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId) =>
        await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

    public async Task<PaginatedList<Enrollment>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize)
    {
        var query = _context.Enrollments.Where(e => e.UserId == userId).OrderByDescending(e => e.EnrolledAt);
        var total = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<Enrollment>(items, total, pageNumber, pageSize);
    }

    public async Task<int> GetEnrollmentCountAsync(Guid courseId) =>
        await _context.Enrollments.CountAsync(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active);

    public async Task<bool> IsEnrolledAsync(Guid userId, Guid courseId) =>
        await _context.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId && e.Status == EnrollmentStatus.Active);

    public void Add(Enrollment enrollment) => _context.Enrollments.Add(enrollment);
    public void Update(Enrollment enrollment) => _context.Enrollments.Update(enrollment);
    public void Remove(Enrollment enrollment) => _context.Enrollments.Remove(enrollment);

    public async Task<int> GetWaitlistCountAsync(Guid courseId) =>
        await _context.Waitlist.CountAsync(w => w.CourseId == courseId);

    public void AddToWaitlist(WaitlistEntry entry) => _context.Waitlist.Add(entry);

    public async Task<WaitlistEntry?> GetNextWaitlistEntryAsync(Guid courseId) =>
        await _context.Waitlist.Where(w => w.CourseId == courseId).OrderBy(w => w.Position).FirstOrDefaultAsync();

    public void RemoveFromWaitlist(WaitlistEntry entry) => _context.Waitlist.Remove(entry);
}
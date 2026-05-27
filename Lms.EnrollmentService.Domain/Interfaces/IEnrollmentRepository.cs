using Lms.EnrollmentService.Application.Common;
using Lms.EnrollmentService.Domain.Entities;

namespace Lms.EnrollmentService.Domain.Interfaces;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id);
    Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId);
    Task<PaginatedList<Enrollment>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize);
    Task<int> GetEnrollmentCountAsync(Guid courseId);
    Task<bool> IsEnrolledAsync(Guid userId, Guid courseId);
    void Add(Enrollment enrollment);
    void Update(Enrollment enrollment);
    void Remove(Enrollment enrollment);

    Task<int> GetWaitlistCountAsync(Guid courseId);
    void AddToWaitlist(WaitlistEntry entry);
    Task<WaitlistEntry?> GetNextWaitlistEntryAsync(Guid courseId);
    void RemoveFromWaitlist(WaitlistEntry entry);
}
using Lms.EnrollmentService.Domain.Common;
using Lms.EnrollmentService.Domain.Enums;

namespace Lms.EnrollmentService.Domain.Entities;

/// <summary>
/// AGGREGATE ROOT: Enrollment
/// Represents a student's enrollment in a course.
/// </summary>
public class Enrollment : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DroppedAt { get; private set; }

    private Enrollment() { }

    public Enrollment(Guid userId, Guid courseId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (courseId == Guid.Empty) throw new ArgumentException("CourseId is required.", nameof(courseId));

        UserId = userId;
        CourseId = courseId;
        Status = EnrollmentStatus.Active;
        EnrolledAt = DateTime.UtcNow;
        SetCreated();
    }

    public void Complete()
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException("Only active enrollments can be completed.");
        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Drop()
    {
        if (Status == EnrollmentStatus.Completed)
            throw new InvalidOperationException("Completed enrollments cannot be dropped.");
        Status = EnrollmentStatus.Dropped;
        DroppedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public bool IsActive => Status == EnrollmentStatus.Active;
}
using Lms.EnrollmentService.Domain.Common;

namespace Lms.EnrollmentService.Domain.Entities;

/// <summary>
/// Represents a student on the waitlist for a full course.
/// </summary>
public class WaitlistEntry : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid Position { get; private set; }
    public DateTime AddedAt { get; private set; }

    private WaitlistEntry() { }

    public WaitlistEntry(Guid userId, Guid courseId, Guid position)
    {
        UserId = userId;
        CourseId = courseId;
        Position = position;
        AddedAt = DateTime.UtcNow;
        SetCreated();
    }
}
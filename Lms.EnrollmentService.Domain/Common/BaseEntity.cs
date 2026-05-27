namespace Lms.EnrollmentService.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    protected void SetCreated() { CreatedAt = DateTime.UtcNow; }
    protected void SetUpdated() { UpdatedAt = DateTime.UtcNow; }
}
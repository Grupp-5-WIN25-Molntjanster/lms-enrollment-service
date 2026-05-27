namespace Lms.EnrollmentService.Application.DTOs;

public class EnrollmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DroppedAt { get; set; }
    public bool CourseHasContent { get; set; }
}
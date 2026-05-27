namespace Lms.EnrollmentService.Application.DTOs;

public class EnrollmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public EnrollmentDto? Enrollment { get; set; }
    public bool AddedToWaitlist { get; set; }
    public int? WaitlistPosition { get; set; }
}
namespace Lms.EnrollmentService.Application.Interfaces;

/// <summary>
/// Publishes enrollment events to Azure Service Bus.
/// Consumed by Notification Service for welcome emails.
/// </summary>
public interface IServiceBusPublisher
{
    Task PublishEnrollmentEventAsync(EnrollmentEventMessage message);
}

public class EnrollmentEventMessage
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string EventType { get; set; } = string.Empty; // Enrolled, Completed, Dropped
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
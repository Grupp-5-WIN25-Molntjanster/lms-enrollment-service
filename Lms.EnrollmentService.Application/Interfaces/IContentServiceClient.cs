namespace Lms.EnrollmentService.Application.Interfaces;

/// <summary>
/// Client for calling Content Service to validate course existence and content.
/// Uses REST + API Key with Polly circuit breaker for resilience.
/// </summary>
public interface IContentServiceClient
{
    Task<bool> CourseHasContentAsync(Guid courseId);
    Task<bool> CourseExistsAsync(Guid courseId);
}
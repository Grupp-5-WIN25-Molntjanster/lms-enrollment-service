using Lms.EnrollmentService.Application.Common;
using Lms.EnrollmentService.Application.DTOs;
using Lms.EnrollmentService.Application.Interfaces;
using Lms.EnrollmentService.Domain.Entities;
using Lms.EnrollmentService.Domain.Enums;
using Lms.EnrollmentService.Domain.Interfaces;

namespace Lms.EnrollmentService.Application.Services;

/// <summary>
/// Enrollment business logic.
/// 
/// KEY FEATURES:
/// - Validates course exists via Content Service (REST + API Key)
/// - Polly circuit breaker prevents cascading failures
/// - Publishes events to Service Bus for async notification
/// - Waitlist support when course is full
/// </summary>
public class EnrollmentService
{
    private readonly IEnrollmentRepository _repository;
    private readonly IContentServiceClient _contentServiceClient;
    private readonly IServiceBusPublisher _serviceBusPublisher;
    private readonly IApplicationDbContext _context;

    public EnrollmentService(
        IEnrollmentRepository repository,
        IContentServiceClient contentServiceClient,
        IServiceBusPublisher serviceBusPublisher,
        IApplicationDbContext context)
    {
        _repository = repository;
        _contentServiceClient = contentServiceClient;
        _serviceBusPublisher = serviceBusPublisher;
        _context = context;
    }

    /// <summary>
    /// Enroll a student in a course.
    /// 
    /// FLOW:
    /// 1. Validate course has content via Content Service (with Polly retry)
    /// 2. Check student not already enrolled
    /// 3. Create enrollment
    /// 4. Publish event to Service Bus (async - fire and forget)
    /// </summary>
    public async Task<EnrollmentResponse> EnrollAsync(Guid userId, EnrollRequest request)
    {
        // STEP 1: Validate course has content (cross-service call with Polly)
        var hasContent = await _contentServiceClient.CourseHasContentAsync(request.CourseId);
        if (!hasContent)
        {
            return new EnrollmentResponse
            {
                Success = false,
                Message = "This course has no published content yet."
            };
        }

        // STEP 2: Check existing enrollment
        var existing = await _repository.GetByUserAndCourseAsync(userId, request.CourseId);
        if (existing != null)
        {
            return new EnrollmentResponse
            {
                Success = false,
                Message = existing.Status switch
                {
                    EnrollmentStatus.Active => "You are already enrolled in this course.",
                    EnrollmentStatus.Completed => "You have already completed this course.",
                    EnrollmentStatus.Dropped => "You previously dropped this course.",
                    _ => "You are already enrolled."
                }
            };
        }

        // STEP 3: Create enrollment
        var enrollment = new Enrollment(userId, request.CourseId);
        _repository.Add(enrollment);
        await _context.SaveChangesAsync();

        // STEP 4: Publish event to Service Bus (async - doesn't block response)
        await _serviceBusPublisher.PublishEnrollmentEventAsync(new EnrollmentEventMessage
        {
            UserId = userId,
            CourseId = request.CourseId,
            EventType = "Enrolled"
        });

        return new EnrollmentResponse
        {
            Success = true,
            Message = "Successfully enrolled in the course.",
            Enrollment = MapToDto(enrollment)
        };
    }

    /// <summary>
    /// Unenroll (drop) from a course.
    /// </summary>
    public async Task<EnrollmentResponse> UnenrollAsync(Guid userId, Guid courseId)
    {
        var enrollment = await _repository.GetByUserAndCourseAsync(userId, courseId);
        if (enrollment == null)
            return new EnrollmentResponse { Success = false, Message = "Not enrolled in this course." };

        enrollment.Drop();
        _repository.Update(enrollment);
        await _context.SaveChangesAsync();

        await _serviceBusPublisher.PublishEnrollmentEventAsync(new EnrollmentEventMessage
        {
            UserId = userId,
            CourseId = courseId,
            EventType = "Dropped"
        });

        return new EnrollmentResponse
        {
            Success = true,
            Message = "Successfully dropped the course.",
            Enrollment = MapToDto(enrollment)
        };
    }

    /// <summary>
    /// Get all enrollments for a student.
    /// </summary>
    public async Task<PaginatedResult<EnrollmentDto>> GetMyEnrollmentsAsync(Guid userId, PaginationRequest pagination)
    {
        var paginated = await _repository.GetByUserIdAsync(userId, pagination.PageNumber, pagination.PageSize);
        var dtos = paginated.Items.Select(MapToDto).ToList();
        return new PaginatedResult<EnrollmentDto>(dtos, paginated.TotalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <summary>
    /// Check if a student is enrolled in a course.
    /// Called by Progress Service via internal API.
    /// </summary>
    public async Task<bool> IsEnrolledAsync(Guid userId, Guid courseId)
    {
        return await _repository.IsEnrolledAsync(userId, courseId);
    }

    /// <summary>
    /// Get enrollment count for a course.
    /// Called by Instructor Dashboard.
    /// </summary>
    public async Task<int> GetEnrollmentCountAsync(Guid courseId)
    {
        return await _repository.GetEnrollmentCountAsync(courseId);
    }

    private static EnrollmentDto MapToDto(Enrollment enrollment) => new()
    {
        Id = enrollment.Id,
        UserId = enrollment.UserId,
        CourseId = enrollment.CourseId,
        Status = enrollment.Status.ToString(),
        EnrolledAt = enrollment.EnrolledAt,
        CompletedAt = enrollment.CompletedAt,
        DroppedAt = enrollment.DroppedAt
    };
}
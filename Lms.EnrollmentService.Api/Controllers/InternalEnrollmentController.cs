using Microsoft.AspNetCore.Mvc;

namespace Lms.EnrollmentService.Api.Controllers;

[ApiController]
[Route("api/internal/enrollments")]
public class InternalEnrollmentController : ControllerBase
{
    private readonly Lms.EnrollmentService.Application.Services.EnrollmentService _service;

    public InternalEnrollmentController(Lms.EnrollmentService.Application.Services.EnrollmentService service) => _service = service;

    [HttpGet("check")]
    public async Task<IActionResult> IsEnrolled([FromQuery] Guid userId, [FromQuery] Guid courseId)
    {
        var enrolled = await _service.IsEnrolledAsync(userId, courseId);
        return Ok(new { userId, courseId, enrolled });
    }

    [HttpGet("course/{courseId}/count")]
    public async Task<IActionResult> GetEnrollmentCount(Guid courseId)
    {
        var count = await _service.GetEnrollmentCountAsync(courseId);
        return Ok(new { courseId, count });
    }
}
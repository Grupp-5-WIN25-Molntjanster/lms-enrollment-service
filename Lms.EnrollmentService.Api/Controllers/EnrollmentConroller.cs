using System.Security.Claims;
using Lms.EnrollmentService.Application.Common;
using Lms.EnrollmentService.Application.DTOs;
using Lms.EnrollmentService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.EnrollmentService.Api.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly Lms.EnrollmentService.Application.Services.EnrollmentService _service;

    public EnrollmentController(Lms.EnrollmentService.Application.Services.EnrollmentService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request)
    {
        // Attempt to read 'sub' claim; fall back to NameIdentifier if middleware mapped the claim.
        var subClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var userId = Guid.Parse(subClaim!.Value);
        var result = await _service.EnrollAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{courseId}")]
    public async Task<IActionResult> Unenroll(Guid courseId)
    {
        // Attempt to read 'sub' claim; fall back to NameIdentifier if middleware mapped the claim.
        var subClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var userId = Guid.Parse(subClaim!.Value);
        var result = await _service.UnenrollAsync(userId, courseId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("my-courses")]
    public async Task<IActionResult> GetMyEnrollments([FromQuery] PaginationRequest pagination)
    {
        // Attempt to read 'sub' claim; fall back to NameIdentifier if middleware mapped the claim.
        var subClaim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var userId = Guid.Parse(subClaim!.Value);
        var result = await _service.GetMyEnrollmentsAsync(userId, pagination);
        return Ok(result);
    }
}
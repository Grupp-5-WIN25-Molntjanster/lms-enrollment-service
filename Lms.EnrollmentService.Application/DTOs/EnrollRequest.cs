using System.ComponentModel.DataAnnotations;

namespace Lms.EnrollmentService.Application.DTOs;

public class EnrollRequest
{
    [Required]
    public Guid CourseId { get; set; }
}
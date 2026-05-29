using FluentAssertions;
using Lms.EnrollmentService.Domain.Entities;

namespace Lms.EnrollmentService.UnitTests.Domain;

public class WaitlistEntryTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var position = Guid.NewGuid();

        var entry = new WaitlistEntry(userId, courseId, position);

        entry.UserId.Should().Be(userId);
        entry.CourseId.Should().Be(courseId);
        entry.Position.Should().Be(position);
        entry.AddedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
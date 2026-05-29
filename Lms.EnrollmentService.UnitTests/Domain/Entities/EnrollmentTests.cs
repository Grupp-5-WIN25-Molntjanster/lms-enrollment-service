using FluentAssertions;
using Lms.EnrollmentService.Domain.Entities;
using Lms.EnrollmentService.Domain.Enums;

namespace Lms.EnrollmentService.UnitTests.Domain;

/// <summary>
/// Unit tests for Enrollment aggregate root.
/// Tests domain business rules without any infrastructure.
/// </summary>
public class EnrollmentTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var enrollment = new Enrollment(_userId, _courseId);

        enrollment.UserId.Should().Be(_userId);
        enrollment.CourseId.Should().Be(_courseId);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.IsActive.Should().BeTrue();
        enrollment.EnrolledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        enrollment.CompletedAt.Should().BeNull();
        enrollment.DroppedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowException()
    {
        Action act = () => new Enrollment(Guid.Empty, _courseId);
        act.Should().Throw<ArgumentException>().WithMessage("*UserId*");
    }

    [Fact]
    public void Create_WithEmptyCourseId_ShouldThrowException()
    {
        Action act = () => new Enrollment(_userId, Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*CourseId*");
    }

    [Fact]
    public void Complete_ActiveEnrollment_ShouldSucceed()
    {
        var enrollment = new Enrollment(_userId, _courseId);

        enrollment.Complete();

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletedAt.Should().NotBeNull();
        enrollment.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        enrollment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Complete_AlreadyCompleted_ShouldThrowException()
    {
        var enrollment = new Enrollment(_userId, _courseId);
        enrollment.Complete();

        Action act = () => enrollment.Complete();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Only active*");
    }

    [Fact]
    public void Complete_Dropped_ShouldThrowException()
    {
        var enrollment = new Enrollment(_userId, _courseId);
        enrollment.Drop();

        Action act = () => enrollment.Complete();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Only active*");
    }

    [Fact]
    public void Drop_ActiveEnrollment_ShouldSucceed()
    {
        var enrollment = new Enrollment(_userId, _courseId);

        enrollment.Drop();

        enrollment.Status.Should().Be(EnrollmentStatus.Dropped);
        enrollment.DroppedAt.Should().NotBeNull();
        enrollment.DroppedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        enrollment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Drop_Completed_ShouldThrowException()
    {
        var enrollment = new Enrollment(_userId, _courseId);
        enrollment.Complete();

        Action act = () => enrollment.Drop();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Completed*");
    }

    [Fact]
    public void Drop_AlreadyDropped_ShouldSucceed()
    {
        var enrollment = new Enrollment(_userId, _courseId);
        enrollment.Drop();

        // Dropping again should work (status stays Dropped)
        enrollment.Drop();
        enrollment.Status.Should().Be(EnrollmentStatus.Dropped);
    }

    [Fact]
    public void IsActive_ShouldReturnCorrectValue()
    {
        var enrollment = new Enrollment(_userId, _courseId);
        enrollment.IsActive.Should().BeTrue();

        enrollment.Complete();
        enrollment.IsActive.Should().BeFalse();

        var enrollment2 = new Enrollment(Guid.NewGuid(), Guid.NewGuid());
        enrollment2.Drop();
        enrollment2.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdatedAt_ShouldBeSet_OnStatusChange()
    {
        var enrollment = new Enrollment(_userId, _courseId);
        enrollment.UpdatedAt.Should().BeNull();

        enrollment.Complete();
        enrollment.UpdatedAt.Should().NotBeNull();
        enrollment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
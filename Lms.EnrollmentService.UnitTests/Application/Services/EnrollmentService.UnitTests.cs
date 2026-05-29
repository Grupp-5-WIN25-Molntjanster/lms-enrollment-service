using FluentAssertions;
using Lms.EnrollmentService.Application.Common;
using Lms.EnrollmentService.Application.DTOs;
using Lms.EnrollmentService.Application.Interfaces;
using Lms.EnrollmentService.Domain.Entities;
using Lms.EnrollmentService.Domain.Enums;
using Lms.EnrollmentService.Domain.Interfaces;
using Moq;
using System.Timers;

namespace Lms.EnrollmentService.UnitTests.Application;

/// <summary>
/// Unit tests for EnrollmentService business logic.
/// All dependencies mocked with Moq.
/// </summary>
public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _repoMock;
    private readonly Mock<IContentServiceClient> _contentClientMock;
    private readonly Mock<IServiceBusPublisher> _serviceBusMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Lms.EnrollmentService.Application.Services.EnrollmentService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();

    public EnrollmentServiceTests()
    {
        _repoMock = new Mock<IEnrollmentRepository>();
        _contentClientMock = new Mock<IContentServiceClient>();
        _serviceBusMock = new Mock<IServiceBusPublisher>();
        _contextMock = new Mock<IApplicationDbContext>();
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new Lms.EnrollmentService.Application.Services.EnrollmentService(
            _repoMock.Object,
            _contentClientMock.Object,
            _serviceBusMock.Object,
            _contextMock.Object);
    }

    // ================================================================
    // ENROLL TESTS
    // ================================================================

    [Fact]
    public async Task EnrollAsync_CourseHasContent_ShouldSucceed()
    {
        // Arrange
        _contentClientMock.Setup(c => c.CourseHasContentAsync(_courseId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByUserAndCourseAsync(_userId, _courseId)).ReturnsAsync((Enrollment?)null);

        // Act
        var result = await _service.EnrollAsync(_userId, new EnrollRequest { CourseId = _courseId });

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Successfully");
        result.Enrollment.Should().NotBeNull();
        result.Enrollment!.UserId.Should().Be(_userId);
        result.Enrollment.CourseId.Should().Be(_courseId);
        result.Enrollment.Status.Should().Be("Active");
        result.AddedToWaitlist.Should().BeFalse();

        _repoMock.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(default), Times.AtLeastOnce);
        _serviceBusMock.Verify(s => s.PublishEnrollmentEventAsync(
            It.Is<EnrollmentEventMessage>(m => m.EventType == "Enrolled")), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_NoContent_ShouldFail()
    {
        // Arrange
        _contentClientMock.Setup(c => c.CourseHasContentAsync(_courseId)).ReturnsAsync(false);

        // Act
        var result = await _service.EnrollAsync(_userId, new EnrollRequest { CourseId = _courseId });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no published content");
        result.Enrollment.Should().BeNull();

        _repoMock.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Never);
        _serviceBusMock.Verify(s => s.PublishEnrollmentEventAsync(It.IsAny<EnrollmentEventMessage>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_AlreadyActive_ShouldFail()
    {
        // Arrange
        var existing = new Enrollment(_userId, _courseId);
        _contentClientMock.Setup(c => c.CourseHasContentAsync(_courseId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByUserAndCourseAsync(_userId, _courseId)).ReturnsAsync(existing);

        // Act
        var result = await _service.EnrollAsync(_userId, new EnrollRequest { CourseId = _courseId });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already enrolled");

        _repoMock.Verify(r => r.Add(It.IsAny<Enrollment>()), Times.Never);
        _serviceBusMock.Verify(s => s.PublishEnrollmentEventAsync(It.IsAny<EnrollmentEventMessage>()), Times.Never);
    }


    // ================================================================
    // UNENROLL TESTS
    // ================================================================

    [Fact]
    public async Task UnenrollAsync_Active_ShouldSucceed()
    {
        // Arrange
        var enrollment = new Enrollment(_userId, _courseId);
        _repoMock.Setup(r => r.GetByUserAndCourseAsync(_userId, _courseId)).ReturnsAsync(enrollment);

        // Act
        var result = await _service.UnenrollAsync(_userId, _courseId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Successfully dropped");
        result.Enrollment!.Status.Should().Be("Dropped");
        enrollment.Status.Should().Be(EnrollmentStatus.Dropped);

        _repoMock.Verify(r => r.Update(It.IsAny<Enrollment>()), Times.Once);
        _serviceBusMock.Verify(s => s.PublishEnrollmentEventAsync(
            It.Is<EnrollmentEventMessage>(m => m.EventType == "Dropped")), Times.Once);
    }

    // ================================================================
    // GET ENROLLMENTS TEST
    // ================================================================

    [Fact]
    public async Task GetMyEnrollmentsAsync_ShouldReturnPaginated()
    {
        // Arrange
        var enrollments = new List<Enrollment>
        {
            new Enrollment(_userId, _courseId),
            new Enrollment(_userId, Guid.NewGuid())
        };
        var paginatedList = new PaginatedList<Enrollment>(
            enrollments, enrollments.Count, 1, 10);

        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, 1, 10)).ReturnsAsync(paginatedList);

        // Act
        var result = await _service.GetMyEnrollmentsAsync(_userId, new PaginationRequest { PageNumber = 1, PageSize = 10 });

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetMyEnrollmentsAsync_Empty_ShouldReturnEmptyList()
    {
        // Arrange
        var paginatedList = new PaginatedList<Enrollment>(
            new List<Enrollment>(), 0, 1, 10);
        _repoMock.Setup(r => r.GetByUserIdAsync(_userId, 1, 10)).ReturnsAsync(paginatedList);

        // Act
        var result = await _service.GetMyEnrollmentsAsync(_userId, new PaginationRequest { PageNumber = 1, PageSize = 10 });

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ================================================================
    // IS ENROLLED TESTS
    // ================================================================

    [Fact]
    public async Task IsEnrolledAsync_Enrolled_ShouldReturnTrue()
    {
        _repoMock.Setup(r => r.IsEnrolledAsync(_userId, _courseId)).ReturnsAsync(true);
        var result = await _service.IsEnrolledAsync(_userId, _courseId);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnrolledAsync_NotEnrolled_ShouldReturnFalse()
    {
        _repoMock.Setup(r => r.IsEnrolledAsync(_userId, _courseId)).ReturnsAsync(false);
        var result = await _service.IsEnrolledAsync(_userId, _courseId);
        result.Should().BeFalse();
    }

    // ================================================================
    // ENROLLMENT COUNT TEST
    // ================================================================

    [Fact]
    public async Task GetEnrollmentCountAsync_ShouldReturnCount()
    {
        _repoMock.Setup(r => r.GetEnrollmentCountAsync(_courseId)).ReturnsAsync(42);
        var result = await _service.GetEnrollmentCountAsync(_courseId);
        result.Should().Be(42);
    }
}
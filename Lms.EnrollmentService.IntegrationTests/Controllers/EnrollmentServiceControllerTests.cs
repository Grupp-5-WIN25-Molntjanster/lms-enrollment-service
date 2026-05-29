using FluentAssertions;
using Lms.EnrollmentService.Application.Common;
using Lms.EnrollmentService.Application.DTOs;
using Lms.EnrollmentService.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Timers;

namespace Lms.EnrollmentService.IntegrationTests.Controllers;

/// <summary>
/// Comprehensive integration tests for EnrollmentController.
/// 
/// JWT CONFIGURATION: Must match appsettings.Development.json EXACTLY.
/// Secret: "dev-secret-key-at-least-32-characters-long!!"
/// </summary>
public class EnrollmentControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testCourseId = Guid.NewGuid();

    private const string JWT_SECRET = "dev-secret-key-at-least-32-characters-long-!!";
    private const string JWT_ISSUER = "lms-auth-service";
    private const string JWT_AUDIENCE = "lms-api";

    public EnrollmentControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(30);
        _client.DefaultRequestHeaders.Clear();
    }

    private string GenerateToken(string role = "Student", Guid? userId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT_SECRET));
        var uid = userId ?? _testUserId;

        var claims = new[]
        {
            new Claim("sub", uid.ToString()),
            new Claim(ClaimTypes.NameIdentifier, uid.ToString()),
            new Claim("email", $"{role.ToLower()}@lms.com"),
            new Claim(ClaimTypes.Email, $"{role.ToLower()}@lms.com"),
            new Claim("role", role),
            new Claim(ClaimTypes.Role, role),
            new Claim("name", $"Test {role}"),
            new Claim(ClaimTypes.Name, $"Test {role}"),
            new Claim("firstName", "Test"),
            new Claim("lastName", role)
        };

        var token = new JwtSecurityToken(
            issuer: JWT_ISSUER,
            audience: JWT_AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthHeader(string role = "Student", Guid? userId = null)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(role, userId));
    }

    private void ClearAuthHeader()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    // ================================================================
    // UNAUTHORIZED TESTS (No JWT)
    // ================================================================

    [Fact]
    public async Task Enroll_WithoutAuth_ShouldReturnUnauthorized()
    {
        ClearAuthHeader();
        var response = await _client.PostAsJsonAsync("/api/enrollments",
            new EnrollRequest { CourseId = _testCourseId });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyEnrollments_WithoutAuth_ShouldReturnUnauthorized()
    {
        ClearAuthHeader();
        var response = await _client.GetAsync("/api/enrollments/my-courses");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unenroll_WithoutAuth_ShouldReturnUnauthorized()
    {
        ClearAuthHeader();
        var response = await _client.DeleteAsync($"/api/enrollments/{_testCourseId}");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    // ================================================================
    // ENROLL TESTS (With Valid JWT)
    // ================================================================

    [Fact]
    public async Task Enroll_WithValidToken_ShouldSucceed()
    {
        SetAuthHeader("Student");
        _factory.ContentServiceClientMock
            .Setup(c => c.CourseHasContentAsync(_testCourseId))
            .ReturnsAsync(true);

        var response = await _client.PostAsJsonAsync("/api/enrollments",
            new EnrollRequest { CourseId = _testCourseId });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Enrollment.Should().NotBeNull();
        result.Enrollment!.CourseId.Should().Be(_testCourseId);
        result.Enrollment.Status.Should().Be("Active");
        result.Enrollment.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task Enroll_CourseHasNoContent_ShouldFail()
    {
        SetAuthHeader("Student");
        var noContentCourseId = Guid.NewGuid();
        _factory.ContentServiceClientMock
            .Setup(c => c.CourseHasContentAsync(noContentCourseId))
            .ReturnsAsync(false);

        var response = await _client.PostAsJsonAsync("/api/enrollments",
            new EnrollRequest { CourseId = noContentCourseId });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("no published content");
    }

    [Fact]
    public async Task Enroll_MultipleDifferentCourses_ShouldSucceed()
    {
        SetAuthHeader("Student");
        var course1 = Guid.NewGuid();
        var course2 = Guid.NewGuid();

        _factory.ContentServiceClientMock
            .Setup(c => c.CourseHasContentAsync(course1))
            .ReturnsAsync(true);
        _factory.ContentServiceClientMock
            .Setup(c => c.CourseHasContentAsync(course2))
            .ReturnsAsync(true);

        var response1 = await _client.PostAsJsonAsync("/api/enrollments",
            new EnrollRequest { CourseId = course1 });
        response1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var response2 = await _client.PostAsJsonAsync("/api/enrollments",
            new EnrollRequest { CourseId = course2 });
        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Enroll_ServiceBusEventPublished_OnSuccess()
    {
        SetAuthHeader("Student");
        _factory.ContentServiceClientMock
            .Setup(c => c.CourseHasContentAsync(_testCourseId))
            .ReturnsAsync(true);

        await _client.PostAsJsonAsync("/api/enrollments",
            new EnrollRequest { CourseId = _testCourseId });

        _factory.ServiceBusPublisherMock.Verify(
            s => s.PublishEnrollmentEventAsync(
                It.Is<EnrollmentEventMessage>(m =>
                    m.UserId == _testUserId &&
                    m.CourseId == _testCourseId &&
                    m.EventType == "Enrolled")),
            Times.Once);
    }

    // ================================================================
    // UNENROLL TESTS
    // ================================================================

    [Fact]
    public async Task Unenroll_NotEnrolled_ShouldFail()
    {
        SetAuthHeader("Student");
        var randomId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/enrollments/{randomId}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("Not enrolled");
    }

    // ================================================================
    // GET MY ENROLLMENTS TESTS
    // ================================================================

    [Fact]
    public async Task GetMyEnrollments_NoEnrollments_ShouldReturnEmpty()
    {
        var newUserId = Guid.NewGuid();
        SetAuthHeader("Student", newUserId);

        var response = await _client.GetAsync("/api/enrollments/my-courses");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<EnrollmentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetMyEnrollments_DifferentUsersSeeOnlyOwnEnrollments()
    {
        // User A enrolls
        var userA = Guid.NewGuid();
        SetAuthHeader("Student", userA);
        _factory.ContentServiceClientMock
            .Setup(c => c.CourseHasContentAsync(_testCourseId))
            .ReturnsAsync(true);
        await _client.PostAsJsonAsync("/api/enrollments",
            new EnrollRequest { CourseId = _testCourseId });

        // User B checks their enrollments
        var userB = Guid.NewGuid();
        SetAuthHeader("Student", userB);
        var response = await _client.GetAsync("/api/enrollments/my-courses");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<EnrollmentDto>>();
        result!.Items.Should().BeEmpty("user B should not see user A's enrollments");
    }

}
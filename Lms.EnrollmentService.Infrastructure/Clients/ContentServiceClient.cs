using Lms.EnrollmentService.Application.Interfaces;
using System.Net.Http.Json;

namespace Lms.EnrollmentService.Infrastructure.Clients;

/// <summary>
/// HTTP Client for calling Content Service.
/// Uses API Key authentication and Polly resilience policies.
/// </summary>
public class ContentServiceClient : IContentServiceClient
{
    private readonly HttpClient _httpClient;

    public ContentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CourseHasContentAsync(Guid courseId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/content/courses/{courseId}/has-content");

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<CourseHasContentResponse>();
            return result?.HasContent ?? false;
        }
        catch (Exception)
        {
            // Polly will handle retries; if all fail, return false
            return false;
        }
    }

    public async Task<bool> CourseExistsAsync(Guid courseId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/content/courses/{courseId}/has-content");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private class CourseHasContentResponse
    {
        public bool HasContent { get; set; }
    }
}
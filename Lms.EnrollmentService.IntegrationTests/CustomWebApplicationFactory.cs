using Lms.EnrollmentService.Application.Interfaces;
using Lms.EnrollmentService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

namespace Lms.EnrollmentService.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IContentServiceClient> ContentServiceClientMock { get; } = new();
    public Mock<IServiceBusPublisher> ServiceBusPublisherMock { get; } = new();

    public CustomWebApplicationFactory()
    {
        // Setup default mock behavior
        ContentServiceClientMock
            .Setup(c => c.CourseHasContentAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "dev-secret-key-at-least-32-characters-long-!!",
                ["Jwt:Issuer"] = "lms-auth-service",
                ["Jwt:Audience"] = "lms-api",
                ["ConnectionStrings:EnrollmentDb"] = "InMemory",
                ["ServiceUrls:ContentService"] = "http://localhost:9999/", // Won't be called
                ["AzureServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                ["AzureServiceBus:EnrollmentEventsQueue"] = "enrollment-events"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core registrations
            var efDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFramework") == true
                         || d.ServiceType == typeof(DbContextOptions<EnrollmentDbContext>)
                         || d.ServiceType == typeof(EnrollmentDbContext)
                         || d.ServiceType == typeof(IApplicationDbContext)
                         || d.ServiceType == typeof(IDbContextFactory<EnrollmentDbContext>))
                .ToList();

            foreach (var d in efDescriptors)
                services.Remove(d);

            // Add InMemory database
            services.AddDbContext<EnrollmentDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<EnrollmentDbContext>());

            // CRITICAL: Remove ALL IContentServiceClient registrations
            services.RemoveAll<IContentServiceClient>();

            // Also remove any HttpClient registrations for ContentServiceClient
            var httpClientDescriptors = services
                .Where(d => d.ServiceType == typeof(HttpClient)
                         || d.ServiceType == typeof(IHttpClientFactory))
                .ToList();
            foreach (var d in httpClientDescriptors)
                services.Remove(d);

            // Add our mock
            services.AddSingleton(ContentServiceClientMock.Object);

            // Remove real ServiceBusPublisher
            services.RemoveAll<IServiceBusPublisher>();
            services.AddSingleton(ServiceBusPublisherMock.Object);
        });
    }
}

public static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors)
            services.Remove(d);
    }
}
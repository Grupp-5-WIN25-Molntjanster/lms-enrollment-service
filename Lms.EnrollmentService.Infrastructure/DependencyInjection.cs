using Lms.EnrollmentService.Application.Interfaces;
using Lms.EnrollmentService.Application.Services;
using Lms.EnrollmentService.Domain.Interfaces;
using Lms.EnrollmentService.Infrastructure.Clients;
using Lms.EnrollmentService.Infrastructure.Messaging;
using Lms.EnrollmentService.Infrastructure.Persistence;
using Lms.EnrollmentService.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.EnrollmentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<EnrollmentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("EnrollmentDb")));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<EnrollmentDbContext>());

        // Repository
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        // Content Service Client with API Key + Polly
        var contentServiceUrl = configuration["ServiceUrls:ContentService"]!;
        var contentServiceApiKey = configuration["InternalApiKeys:ContentService"]!;

        services.AddHttpClient<IContentServiceClient, ContentServiceClient>(client =>
        {
            client.BaseAddress = new Uri(contentServiceUrl);
            client.DefaultRequestHeaders.Add("X-Api-Key", contentServiceApiKey);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(PollyPolicies.GetCombinedPolicy()); // ← Polly resilience

        // Service Bus Publisher
        services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();

        // Application Service
        services.AddScoped<Lms.EnrollmentService.Application.Services.EnrollmentService>();

        return services;
    }
}

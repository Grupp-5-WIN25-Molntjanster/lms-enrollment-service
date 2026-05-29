using Azure.Messaging.ServiceBus;
using Lms.EnrollmentService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Lms.EnrollmentService.Infrastructure.Messaging;

/// <summary>
/// Publishes enrollment events to Azure Service Bus.
/// 
/// QUEUE: enrollment-events
/// CONSUMED BY: Notification Service
/// 
/// WHY SERVICE BUS:
/// - Async processing: enrollment doesn't wait for email
/// - Decoupled: Notification Service can be down, enrollment still works
/// - Reliable: messages persisted until processed
/// </summary>
public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public ServiceBusPublisher(IConfiguration configuration)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"]!;
        var queueName = configuration["ServiceBus:EnrollmentEventsQueue"] ?? "enrollment-events";

        var client = new ServiceBusClient(connectionString);
        _sender = client.CreateSender(queueName);
    }

    public async Task PublishEnrollmentEventAsync(EnrollmentEventMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Subject = message.EventType
        };

        sbMessage.ApplicationProperties.Add("EventType", message.EventType);
        sbMessage.ApplicationProperties.Add("UserId", message.UserId);
        sbMessage.ApplicationProperties.Add("CourseId", message.CourseId);

        await _sender.SendMessageAsync(sbMessage);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
    }
}
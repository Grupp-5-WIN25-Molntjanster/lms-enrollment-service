using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Lms.EnrollmentService.Infrastructure.Resilience;

/// <summary>
/// Polly resilience policies for inter-service communication.
/// 
/// PATTERNS USED:
/// - Retry: Retry 3 times with exponential backoff (2s, 4s, 8s)
/// - Circuit Breaker: Break after 5 consecutive failures for 30 seconds
/// - Timeout: Timeout after 10 seconds
/// 
/// WHY POLLY:
/// - Prevents cascading failures when Content Service is down
/// - Circuit breaker stops calling a failing service (fast fail)
/// - Retry handles transient failures (network blips)
/// - Demonstrates VG-level resilience patterns
/// </summary>
public static class PollyPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()           // 5xx, 408, network errors
            .Or<TimeoutRejectedException>()        // Timeout
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log retry attempt
                    System.Diagnostics.Debug.WriteLine(
                        $"Retry {retryAttempt} after {timespan.TotalSeconds}s due to {outcome.Result?.StatusCode}");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan) =>
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Circuit BREAKER OPEN for {timespan.TotalSeconds}s");
                },
                onReset: () =>
                {
                    System.Diagnostics.Debug.WriteLine("Circuit breaker RESET");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            seconds: 10,
            onTimeoutAsync: (context, timespan, task) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Request TIMEOUT after {timespan.TotalSeconds}s");
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Combined policy: Timeout → Retry → Circuit Breaker
    /// Order matters! Timeout wraps innermost.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(
            GetCircuitBreakerPolicy(),  // Outer: stop calling if too many failures
            GetRetryPolicy(),           // Middle: retry on transient failures
            GetTimeoutPolicy()          // Inner: timeout long requests
        );
    }
}
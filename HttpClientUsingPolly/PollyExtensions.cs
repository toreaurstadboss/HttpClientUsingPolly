using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Extensions.Http;

namespace HttpClientUsingPolly;

public static class PollyExtensions
{

    public static void AddPollyHttpClient(this IServiceCollection services)
    {
        //define retrypolicy
        var retryPolicy =

        //add httpclient with Polly
        services
            .AddHttpClient(GithubEndpoints.HttpClientName)
            .AddHttpMessageHandler(() => new RandomHttpErrorHandler(90))
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<HttpResponseMessage>>();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        retryCount:3,
                        sleepDurationProvider: retryAttempt =>
                        {
                              return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        },
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            if (outcome.Exception is HttpRequestException httpEx)
                            {
                                logger.LogInformation($"Retrying due to HTTP error: {(int?)httpEx!.StatusCode} Waiting {timespan.TotalMilliseconds} ms");
                            }
                            else
                            {
                                logger.LogInformation($"Retrying due to error: {outcome.Exception.Message} Waiting {timespan.TotalMilliseconds} ms");
                            }
                        }
                    );
            });
    }
}

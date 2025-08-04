﻿using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using System.Net;

namespace HttpClientUsingPolly
{
 
    public static class PollyExtensions
    {
     
        public const string RetryResiliencePolicy = "RetryResiliencePolicy";

        static readonly HttpStatusCode[] TransientStatusCodes = new[]
        {
            HttpStatusCode.RequestTimeout,      // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway,          // 502
            HttpStatusCode.ServiceUnavailable,  // 503
            HttpStatusCode.GatewayTimeout       // 504
        };

        public static void AddPollyHttpClient(this IServiceCollection services, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("NamedPolly");

            var httpClientBuilder = services.AddHttpClient(GithubEndpoints.HttpClientName)
                .AddHttpMessageHandler(() => new RandomHttpErrorHandler(80));

            httpClientBuilder
                .AddResilienceHandler("GitHubClientPipeline", (builder) =>
                {
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromSeconds(2),
                        ShouldHandle = args =>
                        {
                            // Retry on HttpRequestException
                            if (args.Outcome.Exception is HttpRequestException)
                            {
                                return ValueTask.FromResult(true);
                            }

                            // Retry on non-user triggered cancellations
                            if (args.Outcome.Exception is TaskCanceledException && !args.Context.CancellationToken.IsCancellationRequested)
                            {
                                return ValueTask.FromResult(true);
                            }                          

                            // Retry on transient status codes
                            if (args.Outcome.Result is HttpResponseMessage response &&
                                TransientStatusCodes.Contains(response.StatusCode))
                            {
                                return ValueTask.FromResult(true);
                            }

                            return ValueTask.FromResult(false); //do not retry otherwise
                        },
                        OnRetry = args =>
                        {
                            var httpEx = args.Outcome.Exception as HttpRequestException;
                            logger.LogInformation($"[KeyedPolicy] Retrying due to: {httpEx?.Message}. Attempt: {args.AttemptNumber}");
                            return default;
                        }
                    });
                });
        }



        public static void AddNamedPollyPipelines(this IServiceCollection services, ILoggerFactory loggerFactory)
        {
            services.AddResiliencePipeline<string>(RetryResiliencePolicy, builder =>
            {
                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                    OnRetry = args =>
                    {
                        var httpEx = args.Outcome.Exception as HttpRequestException;
                        var logger = loggerFactory.CreateLogger("NamedPolly");
                        logger.LogInformation($"[NamedPolicy] Retrying due to: {httpEx?.Message}. Attempt: {args.AttemptNumber}");
                        return default;
                    }
                });
            });
        }
    }
}

using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HttpClientUsingPolly
{

    /// <summary>
    /// Contains helper methods to add support for Polly V9 resilience strategies
    /// </summary>
    public static class PollyExtensions
    {
      
        public static void AddPollyHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient(GithubEndpoints.HttpClientName, client =>
                {
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new RandomHttpErrorHandler(errorChance: 75);
                    handler.InnerHandler = new HttpClientHandler(); // Assign the terminal handler
                    return handler;
                }) //IMPORTANT to make Polly retry here using ConfigurePrimaryHttpMessageHandler and set the Innerhandler to HttpClientHandler
                .AddResilienceHandler(RetryResiliencePolicy, builder =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NamedPolly");

                    var options = CreateRetryStrategyOptions(logger);
                    builder.AddRetry(options);
                });
        }    

        public static void AddNamedPollyPipelines(this IServiceCollection services)
        {
            services.AddResiliencePipeline<string>(RetryResiliencePolicy, builder =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NamedPolly");

                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    DelayGenerator = RetryDelaysClient,
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                    OnRetry = args =>
                    {
                        var httpEx = args.Outcome.Exception as HttpRequestException;
                        logger.LogInformation($"[NamedPolicy] Retrying due to: {httpEx?.Message}. Attempt: {args.AttemptNumber}");
                        return default;
                    }
                });
            });
        }

        private static HttpRetryStrategyOptions CreateRetryStrategyOptions(ILogger logger)
        {
            HttpRetryStrategyOptions options = new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                DelayGenerator = RetryDelaysPipeline,
                ShouldHandle = args =>
                {
                    if (args.Outcome.Exception is HttpRequestException ||
                        args.Outcome.Exception is TaskCanceledException)
                        return ValueTask.FromResult(true);

                    if (args.Outcome.Result is HttpResponseMessage response &&
                        TransientStatusCodes.Contains(response.StatusCode))
                        return ValueTask.FromResult(true);

                    return ValueTask.FromResult(false);
                },
                OnRetry = args =>
                {
                    logger.LogInformation($"Retrying... Attempt {args.AttemptNumber}");
                    return default;
                }
            };

            return options;
        }

        public const string RetryResiliencePolicy = "RetryResiliencePolicy";

        static readonly HttpStatusCode[] TransientStatusCodes = new[]
        {
            HttpStatusCode.RequestTimeout,      // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway,          // 502
            HttpStatusCode.ServiceUnavailable,  // 503
            HttpStatusCode.GatewayTimeout       // 504
        };

        static Func<RetryDelayGeneratorArguments<HttpResponseMessage>, ValueTask<TimeSpan?>>? RetryDelaysPipeline =
             args => CommonDelayGenerator(args.AttemptNumber);

        static Func<RetryDelayGeneratorArguments<object>, ValueTask<TimeSpan?>>? RetryDelaysClient =
             args => CommonDelayGenerator(args.AttemptNumber);


        static ValueTask<TimeSpan?> CommonDelayGenerator(int attemptNumber)
        {
            var delay = attemptNumber switch
            {
                1 => TimeSpan.FromSeconds(1),
                2 => TimeSpan.FromSeconds(2),
                3 => TimeSpan.FromSeconds(4),
                _ => TimeSpan.FromSeconds(0) // fallback, shouldn't hit
            };
            return new ValueTask<TimeSpan?>(delay);
        }

    }
}

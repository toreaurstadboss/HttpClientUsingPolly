using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;

namespace HttpClientUsingPolly
{
 
    public static class PollyExtensions
    {
     
        public const string RetryResiliencePolicy = "RetryResiliencePolicy";



        public static void AddPollyHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient(GithubEndpoints.HttpClientName)
                .AddHttpMessageHandler(() => new RandomHttpErrorHandler(80))
                .AddResilienceHandler("GitHubClientPipeline", (builder, context) =>
                {
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromSeconds(2),
                        OnRetry = args =>
                        {
                            var httpEx = args.Outcome.Exception as HttpRequestException;
                            Console.WriteLine($"[KeyedPolicy] Retrying due to: {httpEx?.Message}. Attempt: {args.AttemptNumber}");
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

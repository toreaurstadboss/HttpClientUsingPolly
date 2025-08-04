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
                .AddHttpMessageHandler(() => new RandomHttpErrorHandler(40));
        }

        public static void AddNamedPollyPipelines(this IServiceCollection services)
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
                        Console.WriteLine($"[NamedPolicy] Retrying due to: {httpEx?.Message}. Attempt: {args.AttemptNumber}");
                        return default;
                    }
                });
            });
        }
    }
}

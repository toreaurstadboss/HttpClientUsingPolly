using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using static HttpClientUsingPolly.Constants.HttpClientNames;

namespace HttpClientUsingPolly
{

    /// <summary>
    /// Contains helper methods to add support for Polly V9 resilience strategies
    /// </summary>
    public static class PollyExtensions
    {

        public static void AddPollyHttpClientWithFallback(this IServiceCollection services)
        {
            services.AddHttpClient(Constants.HttpClientNames.FallbackHttpClientName, client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            })
            .AddResilienceHandler(
                 $"{FallbackHttpClientName}{ResilienceHandlerSuffix}",
                (builder, context) =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.LoggerName);

                builder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = args =>
                    {
                        // Fallback skal trigges ved status 500 eller exception
                        return ValueTask.FromResult(
                            args.Outcome.Result?.StatusCode == HttpStatusCode.InternalServerError ||
                            args.Outcome.Exception is HttpRequestException
                        );
                    },
                    FallbackAction = args =>
                    {
                        logger.LogWarning("Fallback triggered. Returning default response.");

                        var jsonObject = new
                        {
                            message = "Fallback response",
                            source = "Polly fallback",
                            timestamp = DateTime.UtcNow
                        };

                        var json = JsonSerializer.Serialize(jsonObject);

                        var fallbackResponse = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                        };

                        return ValueTask.FromResult(Outcome.FromResult(fallbackResponse));
                    }
                });

                // Inject exceptions in 80% of requests
                builder.AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>()
                {
                    Enabled = true,
                    OutcomeGenerator = static args =>
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                        return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
                    },
                    InjectionRate = 0.8,
                    OnOutcomeInjected = args =>
                    {
                        logger.LogWarning("Outcome returning internal server error");
                        return default;
                    }
                });

            });
        }

        public static void AddPollyHttpClientWithExceptionChaosAndBreaker(this IServiceCollection services)
        {

            services.AddHttpClient(CircuitBreakerHttpClientName, client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            })
            .AddResilienceHandler(
                $"{CircuitBreakerHttpClientName}{ResilienceHandlerSuffix}",
                (builder, context) =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.LoggerName);

                //Add circuit breaker that opens after three consecutive failures and breaks for a duration of ten seconds
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    MinimumThroughput = 3,
                    FailureRatio = 1.0,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(10),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => !r.IsSuccessStatusCode)
                        .Handle<HttpRequestException>(),
                    OnOpened = args =>
                    {
                        logger.LogInformation("Circuit breaker opened");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("Circuit breaker closed");
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger.LogInformation("Circuit breaker half opened");
                        return default;
                    }
                });


                // Inject exceptions in 80% of requests
                builder.AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>()
                {
                    Enabled = true,
                    OutcomeGenerator = static args =>
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                        return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
                    },
                    InjectionRate = 0.8,
                    OnOutcomeInjected = args =>
                    {
                        logger.LogWarning("Outcome returning internal server error");
                        return default;
                    }
                });


            });
        }

        public static void AddPollyHttpClientWithIntendedRetriesAndLatencyAndTimeout(this IServiceCollection services)
        {
            services.AddHttpClient(RetryingTimeoutLatencyHttpClientName, client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            })
            .AddResilienceHandler(
                $"{RetryingTimeoutLatencyHttpClientName}{ResilienceHandlerSuffix}",
            (builder, context) =>
             {
                 var serviceProvider = services.BuildServiceProvider();
                 var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.LoggerName);

                 // Timeout strategy : fail if request takes longer than 1s
                 builder.AddTimeout(new HttpTimeoutStrategyOptions
                 {
                     Timeout = TimeSpan.FromSeconds(1),
                     OnTimeout = args =>
                     {
                         logger.LogWarning($"Timeout after {args.Timeout.TotalSeconds} seconds");
                         return default;
                     }
                 });

                 // Chaos latency: inject 3s delay in 30% of cases
                 builder.AddChaosLatency(new ChaosLatencyStrategyOptions
                 {
                     InjectionRate = 0.5,
                     Latency = TimeSpan.FromSeconds(3),
                     Enabled = true,
                     OnLatencyInjected = args =>
                     {
                         logger.LogInformation("... Injecting a latency of 3 seconds ...");
                         return default;
                     }
                 });

                 // Chaos strategy: inject 500 Internal Server Error in 75% of cases
                 builder.AddChaosOutcome<HttpResponseMessage>(
                     new ChaosOutcomeStrategyOptions<HttpResponseMessage>
                     {
                         InjectionRate = 0.5,
                         Enabled = true,
                         OutcomeGenerator = static args =>
                         {
                             var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                             return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
                         },
                         OnOutcomeInjected = args =>
                         {
                             logger.LogWarning("Outcome returning internal server error");
                             return default;
                         }
                     });
             });

        }

        public static void AddPollyHttpClientWithIntendedRetries(this IServiceCollection services)
        {
            services.AddHttpClient(RetryingHttpClientName, client =>
                {
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
                })
                .AddResilienceHandler("polly-chaos", (builder, context) =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.LoggerName);

                    //Retry strategy
                    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .HandleResult(r => !r.IsSuccessStatusCode)
                            .Handle<HttpRequestException>(),
                        MaxRetryAttempts = 3,
                        DelayGenerator = RetryDelaysPipeline,
                        OnRetry = args =>
                        {
                            logger.LogWarning($"Retrying {args.AttemptNumber} for requesturi {args.Context.GetRequestMessage()?.RequestUri}");
                            return default;
                        }
                    });

                    // Chaos strategy: inject 500 Internal Server Error in 75% of cases
                    builder.AddChaosOutcome<HttpResponseMessage>(
                        new ChaosOutcomeStrategyOptions<HttpResponseMessage>
                        {
                            InjectionRate = 0.75,
                            Enabled = true,
                            OutcomeGenerator = static args =>
                            {
                                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                                return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
                            }
                        });

                });

        }

        public static void AddPollyHttpPipeline(this IServiceCollection services)
        {
            services.AddResiliencePipeline<string>(Constants.ResiliencePolicyPrefix + "-Policy-2", builder =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(Constants.LoggerName);

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

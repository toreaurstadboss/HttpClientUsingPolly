namespace HttpClientUsingPolly
{
    public static class Constants
    {
        public const string LoggerName = "NamedPolly";

        public const string ResiliencePolicyPrefix = "ResiliencePoliy";

        public static class HttpClientNames
        {

            public const string FallbackHttpClientName = nameof(FallbackHttpClientName);

            public const string CircuitBreakerHttpClientName = nameof(CircuitBreakerHttpClientName);

            public const string LatencyAndTimeoutHttpClientName = nameof(LatencyAndTimeoutHttpClientName);

            public const string RetryingHttpClientName = nameof(RetryingHttpClientName);

            public const string RetryingTimeoutLatencyHttpClientName = nameof(RetryingTimeoutLatencyHttpClientName);

            public const string ResilienceHandlerSuffix = "ResilienceHandler";


        }
    }
}

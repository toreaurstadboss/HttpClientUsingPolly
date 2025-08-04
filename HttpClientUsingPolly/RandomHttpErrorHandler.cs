namespace HttpClientUsingPolly
{

    public class RandomHttpErrorHandler : DelegatingHandler
    {

        private readonly Random _random = new();
        private readonly double _errorChance;

        public RandomHttpErrorHandler(double errorChance)
        {
            _errorChance = errorChance;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_random.NextDouble() < (_errorChance / 100))
            {
                // Pick a random transient status code
                var httpErrorsPossible = new[]
                {
                        System.Net.HttpStatusCode.RequestTimeout,        // 408
                        System.Net.HttpStatusCode.InternalServerError,   // 500
                        System.Net.HttpStatusCode.BadGateway,            // 502
                        System.Net.HttpStatusCode.ServiceUnavailable,    // 503
                        System.Net.HttpStatusCode.GatewayTimeout         // 504
                    };

                var chosenStatus = httpErrorsPossible[_random.Next(httpErrorsPossible.Length)];

                 throw new HttpRequestException($"Simulated Http error: {(int)chosenStatus}", null, chosenStatus);

            }

            return await base.SendAsync(request, cancellationToken);
        }

    }
}

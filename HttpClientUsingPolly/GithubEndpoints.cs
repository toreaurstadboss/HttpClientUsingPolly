using Microsoft.AspNetCore.Mvc;
using Polly.Registry;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HttpClientUsingPolly
{

    public static class GithubEndpoints
    {

        public const string RetryingHttpClientName = "RetryingHttpClientName";

        public const string RetryingTimeoutLatencyHttpClientName = "RetryingTimeoutLatencyHttpClientName";

        public static void MapGitHubUserEndpoints(this WebApplication app)
        {
            app.MapGet("/github-v1/{username}", async (
                string username,
                [FromServices] IHttpClientFactory httpClientFactory,
                [FromServices] ResiliencePipelineProvider<string> resiliencePipelineProvider) =>
            {
                string url = $"https://api.github.com/users/{username}";

                using var client = httpClientFactory.CreateClient(RetryingHttpClientName);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));

                var response = await resiliencePipelineProvider.ExecuteWithPolicyAsync(
                    PollyExtensions.RetryResiliencePolicy,
                    ct => client.GetAsync(url, ct));

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(json);

                return Results.Json(user);
            });

            app.MapGet("/test-retry-v1", async (
                [FromServices] IHttpClientFactory httpClientFactory,
                [FromServices] ResiliencePipelineProvider<string> resiliencePipelineProvider) =>
            {
                using var client = httpClientFactory.CreateClient(RetryingHttpClientName);

                var response = await resiliencePipelineProvider.ExecuteWithPolicyAsync(
                    PollyExtensions.RetryResiliencePolicy,
                    ct => client.GetAsync("https://example.com", ct));

                return Results.Json(response);
            });

            //use keyed httpclient and do not go via pipeline provider . the http client fails 75% (due to Simmy Chaos outcome behavior added, see PollyExtensions setup for the client)

            app.MapGet("/github-v2/{username}", async (
               string username,
               [FromServices] IHttpClientFactory httpClientFactory) =>
            {
                string url = $"https://api.github.com/users/{username}";
                using var client = httpClientFactory.CreateClient(RetryingHttpClientName);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(json);
                return Results.Json(user);
            });

            app.MapGet("/test-retry-v2", async (
                [FromServices] IHttpClientFactory httpClientFactory) =>
            {
                using var client = httpClientFactory.CreateClient(RetryingHttpClientName);

                var response = await client.GetAsync("https://example.com");

                return Results.Json(response);
            });


            //use keyed httpclient and do not go via pipeline provider. this http client fails more, with added intended latency for 50% of requests of 2 seconds and added timeout after 1 second

            app.MapGet("/github-v3/{username}", async (
               string username,
               [FromServices] IHttpClientFactory httpClientFactory) =>
            {
                string url = $"https://api.github.com/users/{username}";
                using var client = httpClientFactory.CreateClient(RetryingTimeoutLatencyHttpClientName);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(json);
                return Results.Json(user);
            });

            app.MapGet("/test-v3-more-chaos", async (
            [FromServices] IHttpClientFactory httpClientFactory) =>
            {
                using var client = httpClientFactory.CreateClient(RetryingTimeoutLatencyHttpClientName);

                var response = await client.GetAsync("https://example.com");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Results.Problem(
                        detail: $"Request failed with status code {(int)response.StatusCode}: {response.ReasonPhrase}",
                        statusCode: (int)response.StatusCode,
                        title: "External API Error"
                    );
                }

                var json = await response.Content.ReadAsStringAsync();
                return Results.Json(json);

            });

        }
    }

}

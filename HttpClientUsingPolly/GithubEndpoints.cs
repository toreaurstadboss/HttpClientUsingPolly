using Microsoft.AspNetCore.Mvc;
using Polly.Registry;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HttpClientUsingPolly
{
    public static class GithubEndpoints
    {

        public const string HttpClientName = "GitHubClient";

        public static void MapGitHubUserEndpoints(this WebApplication app)
        {
            app.MapGet("/github/{username}", async (
                string username,
                [FromServices] IHttpClientFactory httpClientFactory,
                [FromServices] ResiliencePipelineProvider<string> resiliencePipelineProvider) =>
            {
                string url = $"https://api.github.com/users/{username}";

                using var client = httpClientFactory.CreateClient(HttpClientName);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));

                var response = await PollyPipelineHelper.ExecuteWithPolicyAsync(
                    resiliencePipelineProvider,
                    PollyExtensions.RetryResiliencePolicy,
                    ct => client.GetAsync(url, ct));

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(json);

                return Results.Json(user);
            });

            app.MapGet("/test-retry", async (
                [FromServices] IHttpClientFactory httpClientFactory,
                [FromServices] ResiliencePipelineProvider<string> resiliencePipelineProvider) =>
            {
                using var client = httpClientFactory.CreateClient(HttpClientName);

                var response = await PollyPipelineHelper.ExecuteWithPolicyAsync(
                    resiliencePipelineProvider,
                    PollyExtensions.RetryResiliencePolicy,
                    ct => client.GetAsync("https://example.com", ct));

                return Results.Json(response);
            });
        }
    }

}

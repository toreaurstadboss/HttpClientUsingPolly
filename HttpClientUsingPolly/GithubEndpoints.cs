using Microsoft.AspNetCore.Mvc;
using Polly.Registry;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HttpClientUsingPolly
{

    public static class GithubEndpoints
    {

        public const string RetryingHttpClientName = "RetryingHttpClientName";

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

            //use keyed httpclient and do not go via pipeline provider 

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
        }
    }

}

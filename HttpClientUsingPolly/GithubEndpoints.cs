using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HttpClientUsingPolly
{

    public static class GithubEndpoints
    {

        public const string HttpClientName = "GitHubClient";

        public static void MapGitHubUserEndpoints(this WebApplication app)
        {
            app.MapGet("/github/{username}", async (string username, [FromServices] IHttpClientFactory httpClientFactory) =>
            {
                string url = $"https://api.github.com/users/{username}";

                using var client = httpClientFactory.CreateClient(HttpClientName);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(json);

                return Results.Json(user);

            });

        }

    }

}

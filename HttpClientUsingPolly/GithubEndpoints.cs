using System.Net.Http.Headers;
using System.Text.Json;

namespace HttpClientUsingPolly
{
  
    
    public static class GithubEndpoints
    {

        public static void MapGithubUserEndpoints(this WebApplication app)
        {
            app.MapGet("/github/{username}", async (string username) =>
            {
                string url = $"https://api.github.com/users/{username}";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Results.Problem(($"Github API error: {response.StatusCode}"));
                }

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(json);

                return Results.Json(user); 

            });

        }

    }

}

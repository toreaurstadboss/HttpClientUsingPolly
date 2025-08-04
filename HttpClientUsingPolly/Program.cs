namespace HttpClientUsingPolly
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddSwaggerGen();

            ILoggerFactory loggerFactory = builder.Services.BuildServiceProvider()!.GetRequiredService<ILoggerFactory>();

            builder.Services.AddPollyHttpClient(loggerFactory);
            builder.Services.AddNamedPollyPipelines(loggerFactory);

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGitHubUserEndpoints();

            app.MapControllers();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.Run();
        }
    }
}

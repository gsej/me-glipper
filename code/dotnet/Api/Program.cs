using Api.Controllers;
using Api.Filters;
using Microsoft.OpenApi;

namespace Api;

public class Program
{
    public static void Main(params string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                policy =>
                {
                    policy.AllowAnyOrigin();
                    policy.AllowAnyMethod();
                    policy.AllowAnyHeader();
                });
        }); 
       
        builder.Services.AddControllers();
        builder.Services.AddSingleton<ApiKeyStore>();
        builder.Services.AddScoped<ApiKeyAuthFilter>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(s =>
        {
            s.SchemaFilter<DoseController.DoseRecordExample>();
            s.CustomSchemaIds(x => x.FullName);
            
            s.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key Authentication"
            });

            s.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("ApiKey", doc),
                    new List<string>()
                }
            });
        });
        
        builder.Services.AddScoped<DataAccess.DoseRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["SqliteConnectionString"];
            return new DataAccess.DoseRepository(connectionString!);
        });

        builder.Services.AddScoped<DataAccess.UserRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["SqliteConnectionString"];
            return new DataAccess.UserRepository(connectionString!);
        });

        var app = builder.Build();

        // Validate the API key configuration at startup so a bad config fails the deploy
        // rather than the first request that hits an authenticated endpoint.
        app.Services.GetRequiredService<ApiKeyStore>();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        app.UseCors("AllowAllOrigins");

        app.MapGet("/", http =>
        {
            http.Response.Redirect("/swagger/index.html", false);
            return Task.CompletedTask;
        });

        app.MapControllers();

        app.Run();
    }
}

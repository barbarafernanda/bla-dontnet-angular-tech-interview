using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.UseCases.Auth;
using TaskFlow.Application.UseCases.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Security;
using TaskFlow.Infrastructure.Startup;

namespace TaskFlow.Api.Shared;

public static class ApiConfiguration
{
    public static void AddTaskFlow(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler(options => { });
        builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddOpenApi();
        builder.Services.AddCors();
        builder.Services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>().Configure<IConfiguration>((options, configuration) =>
            options.AddDefaultPolicy(policy => policy.WithOrigins(configuration["Cors:FrontendOrigin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IConfiguration>((options, configuration) =>
        {
            var (issuer, audience, signingKey) = JwtSettings(configuration);
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero
            };
        });
        builder.Services.AddAuthorization(options => options.AddPolicy("ValidUser", policy => policy.RequireAuthenticatedUser().RequireAssertion(context => Guid.TryParse(context.User.FindFirstValue("sub"), out var id) && id != Guid.Empty)));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDbConnectionFactory>(services => new SqlConnectionFactory(ConnectionString(services.GetRequiredService<IConfiguration>())));
        builder.Services.AddScoped<IUserRepository, SqlUserRepository>();
        builder.Services.AddScoped<ITaskRepository, SqlTaskRepository>();
        builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        builder.Services.AddSingleton<IJwtTokenGenerator>(services =>
        {
            var (issuer, audience, signingKey) = JwtSettings(services.GetRequiredService<IConfiguration>());
            return new JwtTokenGenerator(issuer, audience, signingKey, TimeSpan.FromHours(1), services.GetRequiredService<TimeProvider>());
        });
        builder.Services.AddScoped<RegisterUserUseCase>();
        builder.Services.AddScoped<LoginUseCase>();
        builder.Services.AddScoped<CreateTaskUseCase>();
        builder.Services.AddScoped<ListTasksUseCase>();
        builder.Services.AddScoped<GetTaskByIdUseCase>();
        builder.Services.AddScoped<UpdateTaskUseCase>();
        builder.Services.AddScoped<DeleteTaskUseCase>();
        builder.Services.AddSingleton<SqlSchemaInitializer>(services =>
        {
            var connection = new SqlConnectionStringBuilder(ConnectionString(services.GetRequiredService<IConfiguration>()));
            var databaseName = connection.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("ConnectionStrings:TaskFlow must specify a database.");
            connection.InitialCatalog = "master";
            return new SqlSchemaInitializer(new SqlConnectionFactory(connection.ConnectionString), databaseName);
        });
        builder.Services.AddSingleton<DemoDataSeeder>();
        builder.Services.AddHostedService<DatabaseInitializationService>();
    }

    private static string ConnectionString(IConfiguration configuration) => configuration.GetConnectionString("TaskFlow") ?? throw new InvalidOperationException("ConnectionStrings:TaskFlow is required.");

    private static (string Issuer, string Audience, string Key) JwtSettings(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is required.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is required.");
        var key = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
        return (issuer, audience, key);
    }

    public static void UseTaskFlow(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapOpenApi();
    }

    public static IActionResult Error<T>(ControllerBase controller, Result<T> result)
    {
        var status = result.Error switch
        {
            ErrorCode.Validation => StatusCodes.Status400BadRequest,
            ErrorCode.Conflict => StatusCodes.Status409Conflict,
            ErrorCode.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCode.NotFound => StatusCodes.Status404NotFound,
            _ => throw new InvalidOperationException("Unknown application error.")
        };
        return controller.Problem(statusCode: status, detail: result.Message);
    }

    public static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);
}

internal sealed class DatabaseInitializationService(IServiceProvider services, IHostEnvironment environment) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsEnvironment("Testing")) return;
        await services.GetRequiredService<SqlSchemaInitializer>().InitializeAsync(cancellationToken);
        await services.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

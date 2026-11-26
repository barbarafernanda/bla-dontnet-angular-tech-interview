using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Auth.Api.Tests;

public sealed class AuthControllerTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly AuthApiFactory _factory;

    public AuthControllerTests()
    {
        _factory = new AuthApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_returns_created_location_and_safe_profile()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Alice Example",
            email = "alice@example.test",
            password = "StrongPassword!123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var profile = await response.Content.ReadFromJsonAsync<UserProfile>();
        profile.Should().NotBeNull();
        profile!.Id.Should().NotBeEmpty();
        profile.FullName.Should().Be("Alice Example");
        profile.Email.Should().Be("alice@example.test");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        (await response.Content.ReadAsStringAsync()).Should().NotContain("PasswordHash");
    }

    [Fact]
    public async Task Register_rejects_case_insensitive_duplicate_email_with_problem_details()
    {
        var request = new { fullName = "Alice Example", email = "alice@example.test", password = "StrongPassword!123" };
        (await _client.PostAsJsonAsync("/api/auth/register", request)).StatusCode.Should().Be(HttpStatusCode.Created);
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Alice Duplicate", email = "ALICE@example.test", password = "StrongPassword!123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(409);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("", "alice@example.test", "StrongPassword!123")]
    [InlineData("Alice", "not-an-email", "StrongPassword!123")]
    [InlineData("Alice", "alice@example.test", "")]
    public async Task Register_returns_problem_details_for_invalid_input(string fullName, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { fullName, email, password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(400);
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_returns_signed_token_and_expiry_for_valid_credentials()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Alice Example", email = "alice@example.test", password = "StrongPassword!123"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "alice@example.test", password = "StrongPassword!123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token!.Token.Should().NotBeNullOrWhiteSpace();
        token.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_returns_same_unauthorized_problem_for_unknown_user_and_bad_password()
    {
        var unknown = await _client.PostAsJsonAsync("/api/auth/login", new { email = "unknown@example.test", password = "wrong" });
        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var unknownProblem = await unknown.Content.ReadFromJsonAsync<ProblemDetails>();

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Alice Example", email = "alice@example.test", password = "StrongPassword!123"
        });
        var wrongPassword = await _client.PostAsJsonAsync("/api/auth/login", new { email = "alice@example.test", password = "wrong" });

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var wrongProblem = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>();
        unknownProblem!.Detail.Should().Be(wrongProblem!.Detail);
    }

    [Fact]
    public async Task Public_status_is_anonymous_and_returns_utc_status()
    {
        var response = await _client.GetAsync("/api/auth/public-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PublicStatus>();
        body!.Status.Should().Be("online");
        body.UtcNow.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Me_requires_authentication_and_returns_only_claim_profile()
    {
        (await _client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Alice Example", email = "alice@example.test", password = "StrongPassword!123"
        });
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "alice@example.test", password = "StrongPassword!123"
        });
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfile>();
        profile!.Email.Should().Be("alice@example.test");
        profile.FullName.Should().Be("Alice Example");
        (await response.Content.ReadAsStringAsync()).Should().NotContain("PasswordHash");
    }

    [Fact]
    public void Endpoints_expose_authentication_metadata_for_public_and_protected_actions()
    {
        var endpoints = _factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("api/auth", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        endpoints.Should().HaveCount(4);
        endpoints.Where(endpoint => endpoint.RoutePattern.RawText != "api/auth/me")
            .Should().OnlyContain(endpoint => endpoint.Metadata.Any(metadata => metadata.GetType().Name == "AllowAnonymousAttribute"));
        var me = endpoints.Single(endpoint => endpoint.RoutePattern.RawText == "api/auth/me");
        me.Metadata.Should().Contain(metadata => metadata.GetType().Name == "AuthorizeAttribute");
        me.Metadata.Should().NotContain(metadata => metadata.GetType().Name == "AllowAnonymousAttribute");
    }

    [Fact]
    public async Task Api_exposes_openapi_and_restricts_cors_to_configured_frontend_origin()
    {
        (await _client.GetAsync("/openapi/v1.json")).StatusCode.Should().Be(HttpStatusCode.OK);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/public-status");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        var allowed = await _client.SendAsync(request);
        allowed.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:4200");

        using var disallowedRequest = new HttpRequestMessage(HttpMethod.Options, "/api/auth/public-status");
        disallowedRequest.Headers.Add("Origin", "https://untrusted.example");
        disallowedRequest.Headers.Add("Access-Control-Request-Method", "GET");
        var disallowed = await _client.SendAsync(disallowedRequest);
        disallowed.Headers.Should().NotContain(header => header.Key == "Access-Control-Allow-Origin");
    }

    private sealed record UserProfile(Guid Id, string FullName, string Email, DateTimeOffset CreatedAt);
    private sealed record TokenResponse(string Token, DateTimeOffset ExpiresAtUtc);
    private sealed record PublicStatus(string Status, DateTimeOffset UtcNow);
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-only-key-that-is-at-least-thirty-two-bytes-long";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "TaskFlow.Tests",
            ["Jwt:Audience"] = "TaskFlow.Tests.Client",
            ["Jwt:SigningKey"] = SigningKey,
            ["ConnectionStrings:TaskFlow"] = "Server=localhost;Database=TaskFlowDb;Trusted_Connection=True;TrustServerCertificate=True"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        });
    }
}

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.SingleOrDefault(user => user.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.GetValueOrDefault(email));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (_users.ContainsKey(user.Email))
            throw new DuplicateEmailException("An account with this email already exists.");
        _users.Add(user.Email, user);
        return Task.CompletedTask;
    }
}

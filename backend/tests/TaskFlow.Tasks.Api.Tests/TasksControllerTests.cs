using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Repositories;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Tasks.Api.Tests;

public sealed class TasksControllerTests : IClassFixture<TasksApiFactory>
{
    private const string TestSigningKey = "test-only-key-that-is-at-least-thirty-two-bytes-long";
    private static readonly Guid OwnerId = Guid.Parse("c5a95e15-e0c5-4588-9d8b-2754c8157151");
    private static readonly Guid OtherUserId = Guid.Parse("02b53cff-a192-4736-896d-6d3db65af974");
    private static readonly Guid ExistingTaskId = Guid.Parse("1330749b-796e-432a-abd5-d0b88609180f");
    private readonly HttpClient _client;
    private readonly TasksApiFactory _factory;

    public TasksControllerTests(TasksApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task All_task_endpoints_reject_missing_or_invalid_tokens()
    {
        (await _client.GetAsync("/api/tasks")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.GetAsync($"/api/tasks/{ExistingTaskId}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.PostAsJsonAsync("/api/tasks", ValidTask())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.PutAsJsonAsync($"/api/tasks/{ExistingTaskId}", ValidTask())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.DeleteAsync($"/api/tasks/{ExistingTaskId}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");
        (await _client.GetAsync("/api/tasks")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_returns_only_authenticated_users_tasks()
    {
        SeedOwnedTasks();
        Authenticate(OwnerId);

        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<TaskResponse[]>();
        tasks.Should().ContainSingle();
        tasks![0].Id.Should().Be(ExistingTaskId);
        tasks[0].Title.Should().Be("Owned task");
        tasks.Should().NotContain(task => task.Title == "Other user's task");
    }

    [Fact]
    public async Task Get_by_id_returns_owned_task_and_hides_missing_or_foreign_task()
    {
        SeedOwnedTasks();
        Authenticate(OwnerId);
        (await _client.GetAsync($"/api/tasks/{ExistingTaskId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        var foreignId = _factory.Tasks.GetForeignTaskId();
        var foreign = await _client.GetAsync($"/api/tasks/{foreignId}");
        var missing = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await foreign.Content.ReadFromJsonAsync<ProblemDetails>())!.Status.Should().Be(404);
    }

    [Fact]
    public async Task Create_returns_created_task_and_location_without_accepting_caller_user_id()
    {
        Authenticate(OwnerId);
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "New task", description = "description", status = "Pending",
            dueDate = DateTimeOffset.UtcNow.AddDays(2), userId = OtherUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();
        created!.Title.Should().Be("New task");
        created.UserId.Should().Be(OwnerId);
        created.Status.Should().Be("Pending");
    }

    [Theory]
    [InlineData("", "Pending")]
    [InlineData("Task title", "NotAStatus")]
    public async Task Create_returns_problem_details_for_invalid_task(string title, string status)
    {
        Authenticate(OwnerId);
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title, description = (string?)null, status, dueDate = DateTimeOffset.UtcNow.AddDays(2)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(400);
        problem.Type.Should().NotBeNullOrWhiteSpace();
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task Update_changes_owned_task_and_returns_not_found_for_other_users_task()
    {
        SeedOwnedTasks();
        Authenticate(OwnerId);
        var update = new { title = "Updated task", description = "changed", status = "InProgress", dueDate = DateTimeOffset.UtcNow.AddDays(3) };
        var response = await _client.PutAsJsonAsync($"/api/tasks/{ExistingTaskId}", update);
        var foreignId = _factory.Tasks.GetForeignTaskId();
        var foreign = await _client.PutAsJsonAsync($"/api/tasks/{foreignId}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TaskResponse>();
        body!.Title.Should().Be("Updated task");
        body.Status.Should().Be("InProgress");
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_removes_owned_task_and_returns_not_found_for_missing_or_foreign_task()
    {
        SeedOwnedTasks();
        Authenticate(OwnerId);
        var deleted = await _client.DeleteAsync($"/api/tasks/{ExistingTaskId}");
        var foreignId = _factory.Tasks.GetForeignTaskId();
        var foreign = await _client.DeleteAsync($"/api/tasks/{foreignId}");
        var missing = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _factory.Tasks.ListByUserIdAsync(OwnerId)).Should().BeEmpty();
    }

    [Fact]
    public void Every_task_endpoint_is_authorized_and_no_task_endpoint_is_anonymous()
    {
        var endpoints = _factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("api/tasks", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        endpoints.Should().HaveCount(5);
        endpoints.Should().OnlyContain(endpoint => endpoint.Metadata.Any(metadata => metadata.GetType().Name == "AuthorizeAttribute"));
        endpoints.Should().OnlyContain(endpoint => endpoint.Metadata.All(metadata => metadata.GetType().Name != "AllowAnonymousAttribute"));
    }

    [Fact]
    public async Task Tasks_api_exposes_openapi_and_restricts_cors()
    {
        (await _client.GetAsync("/openapi/v1.json")).StatusCode.Should().Be(HttpStatusCode.OK);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        var response = await _client.SendAsync(request);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:4200");
        response.Headers.Should().NotContain(header => header.Key == "Access-Control-Allow-Credentials");
    }

    private void SeedOwnedTasks() => _factory.Tasks.Seed(
        new TaskItem(ExistingTaskId, OwnerId, "Owned task", null, TaskStatus.Pending,
            DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
        new TaskItem(Guid.Parse("fdf4560d-8193-487b-b7b9-bd8f52e146c7"), OtherUserId, "Other user's task", null,
            TaskStatus.Pending, DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

    private void Authenticate(Guid userId) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(userId));

    private static string CreateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var token = new JwtSecurityToken("TaskFlow.Tests", "TaskFlow.Tests.Client",
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(30),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static object ValidTask() => new
    {
        title = "Valid task", description = (string?)null, status = "Pending", dueDate = DateTimeOffset.UtcNow.AddDays(2)
    };

    private sealed record TaskResponse(Guid Id, Guid UserId, string Title, string? Description, string Status,
        DateTimeOffset DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
}

public sealed class TasksApiFactory : WebApplicationFactory<Program>
{
    private const string TestSigningKey = "test-only-key-that-is-at-least-thirty-two-bytes-long";
    public InMemoryTaskRepository Tasks { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "TaskFlow.Tests",
            ["Jwt:Audience"] = "TaskFlow.Tests.Client",
            ["Jwt:SigningKey"] = TestSigningKey,
            ["ConnectionStrings:TaskFlow"] = "Server=localhost;Database=TaskFlowDb;Trusted_Connection=True;TrustServerCertificate=True"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITaskRepository>();
            services.AddSingleton(Tasks);
            services.AddSingleton<ITaskRepository>(provider => provider.GetRequiredService<InMemoryTaskRepository>());
        });
    }
}

public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly Dictionary<Guid, TaskItem> _tasks = [];

    public void Seed(params TaskItem[] tasks)
    {
        _tasks.Clear();
        foreach (var task in tasks)
            _tasks.Add(task.Id, task);
    }

    public Guid GetForeignTaskId() => _tasks.Values.Single(task => task.UserId != Guid.Parse("c5a95e15-e0c5-4588-9d8b-2754c8157151")).Id;

    public Task<IReadOnlyList<TaskItem>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaskItem>>(_tasks.Values.Where(task => task.UserId == userId).ToArray());

    public Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tasks.TryGetValue(id, out var task) && task.UserId == userId ? task : null);

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _tasks.Add(task.Id, task);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(task.Id, out var existing) || existing.UserId != userId)
            return Task.FromResult(false);
        _tasks[task.Id] = task;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tasks.TryGetValue(id, out var task) && task.UserId == userId && _tasks.Remove(id));
    }
}

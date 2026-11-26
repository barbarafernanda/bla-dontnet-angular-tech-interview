# GenAI assessment: TaskFlow REST API

## Prompt and provenance

The primary prompt artifact is [`.github/sdd_generation_prompt_github_copilot.md`](.github/sdd_generation_prompt_github_copilot.md). Its domain instruction is:

> Domain: **Task Management System ("TaskFlow")** — aligns the main full-stack app with the mandatory GenAI assessment domain.
> Include an informal User Story covering account registration, login, and full CRUD management of personal tasks (`title`, `description`, `status`, `due_date`, linked to `User`).

The full artifact also specifies raw ADO.NET, SQL Server Express, separate authenticated Tasks and Auth APIs, and RED/GREEN tests. Per the owner's clarification, this current version—updated to remove the container-related scope—is the prompt version assessed here; no claim is made about earlier revisions. Follow-up requests authorized execution of the tasks and changed the schema to use `VARCHAR` except for `PasswordHash`, with `Description VARCHAR(8000)`.

## Representative generated output

The [tasks controller](backend/src/TaskFlow.Tasks.Api/Controllers/TasksController.cs) routes task operations to use cases. This excerpt from the current implementation shows creation using the authenticated claim rather than a caller-supplied owner ID:

```csharp
[HttpPost]
public async Task<IActionResult> Create(TaskWriteRequest request, CancellationToken cancellationToken)
{
    var result = await create.ExecuteAsync(ApiConfiguration.UserId(User), request, cancellationToken);
    return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value) : ApiConfiguration.Error(this, result);
}
```

This is the reviewed implementation, not a purported untouched model response. See `TaskWriteRequest` in [Requests.cs](backend/src/TaskFlow.Application/Dtos/Requests.cs) and the [schema initializer](backend/src/TaskFlow.Infrastructure/Startup/SqlSchemaInitializer.cs) for the corresponding request and SQL fields.

## Validation and review

- **Automated tests:** The recorded full backend run (`dotnet test backend\TaskFlow.sln --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false --verbosity quiet`) passed 95 tests. `npm.cmd test -- --watch=false` passed 11 frontend tests. The task plan records RED/GREEN phases and the exact approved locked-test corrections.
- **Database:** The initializer and seeder were run twice against a temporary SQL Express database. The result remained one demo user and five distinct tasks, and the stored demo password hash was verified. The temporary database was removed.
- **Exploratory/API checks:** The `.http` examples were manually reproduced via PowerShell against an isolated SQL Express database; they were not run through the Visual Studio HTTP runner. Local acceptance checks covered registration, login, CRUD, timestamps, invalid input, and cross-user GET/PUT/DELETE. A headless browser exercised registration, sign-in, CRUD, the public/protected endpoint panel, and demo sign-in with no console warnings or errors in that run.
- **Authentication and authorization review:** Focused code review and API tests checked JWT issuer, audience, signature, expiry, user claim validation, owner-scoped repository operations, and anonymous/public endpoint metadata. This is not a formal penetration test or a claim that every security issue was assessed.
- **Immutability:** Where Git was unavailable, the owner authorized SHA-256 comparisons of locked test files. The final documented comparison covered 26 backend and six frontend test files; owner-approved exceptions are recorded in [the plan](docs/sdd/03-tdd-and-tasks.md).

## Corrections and decisions

The implementation was iteratively adjusted against the specification and tests: owner-scoped queries and `404` responses prevent exposing another user's tasks; SQL uses explicitly typed parameters; errors are mapped to HTTP `ProblemDetails`; and JWT validation, CORS, and a token-scoped frontend interceptor were wired explicitly. NuGet audit identified a vulnerable transitive OpenAPI dependency during implementation and the package was updated. These are engineering changes to generated work, not evidence that an earlier model output contained each individual defect.

The owner approved specific test corrections for authentication metadata, isolated test state, Angular test setup, and component selectors. Existing tests were otherwise frozen. See the [task plan](docs/sdd/03-tdd-and-tasks.md) for phase-specific details rather than interpreting these corrections as blanket permission to change tests.

## Edge cases and residual risks

| Case | Current behavior and evidence |
|---|---|
| Duplicate email | Registration checks for an existing user and maps a duplicate to `409`; see [auth use cases](backend/src/TaskFlow.Application/UseCases/Auth/AuthUseCases.cs) and Auth API tests. |
| Invalid status | The status parser and task use cases reject unknown status values; API tests expect `400` with problem details. |
| Past due dates | No future-date restriction is implemented: past dates are allowed, including for seeded completed tasks. A future-date policy would require an explicit specification change and tests. |
| Missing or invalid JWT | Protected endpoints require bearer authentication; API tests check `401`. A separate local browser check verified logout and redirect after an authenticated API returns `401`. |
| Cross-user access | Task repository operations use the claim-derived user ID; missing or foreign tasks return `404`. Local checks covered GET, PUT, and DELETE for another user. |
| Empty or whitespace-only title | Domain validation rejects it; see [TaskItem.cs](backend/src/TaskFlow.Domain/Entities/TaskItem.cs) and task API tests. |
| Demo and transport | Demo credentials are publicly documented and local examples use HTTP; they are not safe for a public production deployment. The SPA keeps tokens in memory and has no refresh-token flow. |

Tests and exploratory checks are evidence for the cases exercised, not proof of absence of vulnerabilities. `VARCHAR` columns follow the database collation/code page; deployments needing unrestricted Unicode data should revisit that owner-requested schema choice.

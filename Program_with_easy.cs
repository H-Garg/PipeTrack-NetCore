using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

// List runs with optional filters: ?status=Running&branch=main&author=akshat&q=commitOrTitle
app.MapGet("/api/runs", (string? status, string? branch, string? author, string? q) =>
{
    var runs = InMemoryRunStore.All();

    if (!string.IsNullOrWhiteSpace(status) &&
        Enum.TryParse<RunStatus>(status, true, out var parsed))
        runs = runs.Where(r => r.Status == parsed);

    if (!string.IsNullOrWhiteSpace(branch))
        runs = runs.Where(r => r.Branch.Equals(branch, StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(author))
        runs = runs.Where(r => r.Author.Equals(author, StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(q))
        runs = runs.Where(r =>
            r.Commit.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            r.Title.Contains(q, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(runs);
});

app.MapGet("/api/runs/{id:int}", (int id) =>
{
    var run = InMemoryRunStore.Get(id);
    return run is null ? Results.NotFound() : Results.Ok(run);
});

// Create a new run quickly (POST body minimal)
public record CreateRunDto(string Branch, string Commit, string Title, string Author);

app.MapPost("/api/runs", (CreateRunDto dto) =>
{
    var run = new PipelineRun(
        Id: InMemoryRunStore.NextId(),
        Branch: dto.Branch,
        Commit: dto.Commit,
        Title: dto.Title,
        Author: dto.Author,
        StartedAt: DateTimeOffset.UtcNow,
        Status: RunStatus.Queued,
        Stages: new()
        {
            new("Checkout", StageStatus.Queued),
            new("Build", StageStatus.Queued),
            new("Test", StageStatus.Queued),
            new("Deploy", StageStatus.Queued),
            new("Verify", StageStatus.Queued),
        }
    );
    InMemoryRunStore.Add(run);
    return Results.Created($"/api/runs/{run.Id}", run);
});

// Update run status quickly
public record UpdateRunStatusDto(string Status);

app.MapPatch("/api/runs/{id:int}/status", (int id, UpdateRunStatusDto dto) =>
{
    if (!Enum.TryParse<RunStatus>(dto.Status, true, out var st))
        return Results.BadRequest(new { error = "Invalid status. Use Queued, Running, Passed, Failed." });

    var ok = InMemoryRunStore.UpdateStatus(id, st);
    return ok ? Results.NoContent() : Results.NotFound();
});

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

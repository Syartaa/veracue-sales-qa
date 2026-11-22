using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.Dev;
using VeraCue.Api.Services.Evaluation;
using VeraCue.Api.Services.Health;
using VeraCue.Api.Services.Templates;
using VeraCue.Api.Services.Transcription;

var builder = WebApplication.CreateBuilder(args);

// Render tells the container which port to listen on via the PORT environment variable.
// Locally PORT isn't set, so launchSettings.json (http://localhost:5000) is used instead.
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// ---------- Services ----------

// PostgreSQL (Neon). The connection string is a secret: locally it comes from
// .NET User Secrets, in production from Render's environment variables
// (ConnectionStrings__DefaultConnection). It is never stored in appsettings or Git.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Set it with: " +
        "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<value>\"");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Feature services. Scoped = one instance per HTTP request, like the DbContext they use.
// TimeProvider gives "now" in one replaceable place (tests can fake the clock).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TemplateService>();

// Send enums as strings ("ReadyForReview", "Met"), matching the frontend's TypeScript types.
// Set twice: controllers and minimal-API endpoints (Results.Ok) have separate JSON settings.
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// AI provider clients. Typed HttpClients: one configured client per provider,
// created and pooled by IHttpClientFactory. API keys come from User Secrets / Render.
builder.Services.AddHttpClient<DeepgramTranscriber>(client =>
{
    client.BaseAddress = new Uri("https://api.deepgram.com/");
    client.Timeout = TimeSpan.FromMinutes(5); // long recordings take a while to transcribe
});
builder.Services.AddHttpClient<GroqEvaluator>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

// OpenAPI description of the API (served in Development only, see below).
builder.Services.AddOpenApi();

// Errors are returned in the standard ProblemDetails JSON format (RFC 9457),
// so the frontend can read every error the same way.
builder.Services.AddProblemDetails();

// Health checks, grouped by tag:
//  "db"        -> GET /api/health            (Render's health check: API + database)
//  "providers" -> GET /api/health/providers  (can this server reach Deepgram and Groq?)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["db"])
    .AddCheck<DeepgramHealthCheck>("deepgram", tags: ["providers"])
    .AddCheck<GroqHealthCheck>("groq", tags: ["providers"]);

// CORS: only the VeraCue frontend may call this API from a browser.
// FRONTEND_ORIGIN can hold several origins separated by commas (e.g. local + Vercel).
var allowedOrigins = (builder.Configuration["FRONTEND_ORIGIN"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// ---------- Database migrations ----------
// Production (Render) brings its database up to date on startup, so every deploy ships
// code and schema together. Locally we run "dotnet ef database update" ourselves instead.
// Override with APPLY_MIGRATIONS_ON_STARTUP=true/false if needed.
var applyMigrations = app.Configuration.GetValue<bool?>("APPLY_MIGRATIONS_ON_STARTUP")
    ?? !app.Environment.IsDevelopment();

if (applyMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    app.Logger.LogInformation("Applying database migrations...");
    // Safe to run every start: only migrations the database hasn't had yet are applied.
    // If this fails, the app doesn't start, so Render keeps the previous version running.
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Database is up to date.");
}

// ---------- HTTP pipeline (order matters) ----------

// Unhandled exceptions -> 500 ProblemDetails; empty error responses (404, 405...) -> ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // GET /openapi/v1.json
    app.MapDevEndpoints(); // Phase 1b provider tests: /api/dev/transcribe, /api/dev/evaluate
}

// No UseHttpsRedirection: locally we use plain http, and on Render HTTPS is
// handled by Render's proxy in front of the container.

app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
});
app.MapHealthChecks("/api/health/providers", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("providers"),
    ResponseWriter = HealthResponseWriter.WriteJson,
});

app.Run();

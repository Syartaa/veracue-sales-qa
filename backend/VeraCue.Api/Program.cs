using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.Dev;
using VeraCue.Api.Services.Evaluation;
using VeraCue.Api.Services.Transcription;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

// PostgreSQL (Neon). The connection string is a secret: locally it comes from
// .NET User Secrets, in production from Render's environment variables
// (ConnectionStrings__DefaultConnection). It is never stored in appsettings or Git.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Set it with: " +
        "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<value>\"");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

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

// /api/health endpoint: "Healthy" only when the API can also reach the database.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

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
app.MapHealthChecks("/api/health");

app.Run();

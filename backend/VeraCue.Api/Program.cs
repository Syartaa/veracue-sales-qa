using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;

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

builder.Services
    .AddControllers()
    // Send enums as strings ("ReadyForReview"), matching the frontend's TypeScript types.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
}

// No UseHttpsRedirection: locally we use plain http, and on Render HTTPS is
// handled by Render's proxy in front of the container.

app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();

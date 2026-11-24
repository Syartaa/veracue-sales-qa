using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VeraCue.Api.Services.Auth;
using VeraCue.Api.Data;
using VeraCue.Api.Services;
using VeraCue.Api.Services.Calls;
using VeraCue.Api.Services.Evaluation;
using VeraCue.Api.Services.Health;
using VeraCue.Api.Services.Storage;
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

// Private recording storage on a persistent disk (Render Disk in production).
builder.Services.AddSingleton<AudioStorage>();

// ---------- Sign-in (JWT) ----------
// The browser signs in once (POST /api/auth/login) and sends the signed token with every request:
//   Authorization: Bearer <token>
// Tokens instead of cookies because the website (vercel.app) and API (onrender.com) are
// different sites, and browsers increasingly block cookies between sites.
var authSettings = new AuthSettings(builder.Configuration); // fails fast if JWT_SIGNING_KEY is missing
builder.Services.AddSingleton(authSettings);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<AudioLinkSigner>();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the standard claim names ("sub", "email", "name")
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = AuthSettings.Issuer,
            ValidAudience = AuthSettings.Audience,
            IssuerSigningKey = authSettings.SigningKey,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// Secure by default: EVERY endpoint requires a signed-in user unless it explicitly opts out
// with [AllowAnonymous] (only login, the health checks and signed audio links do).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Slow down password guessing: at most 5 sign-in attempts per minute per IP address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

// On Render, requests arrive through Render's proxy: read the real visitor IP (for the
// rate limit) and scheme from the X-Forwarded-* headers the proxy adds.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear(); // the container is only reachable through Render's proxy
    options.KnownProxies.Clear();
});

// Calls: upload/status service, plus the background pipeline that transcribes them.
// Queue = singleton (one shared list); worker = hosted service that runs for the app's lifetime.
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<CallService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<CallProcessor>();
builder.Services.AddSingleton<CallProcessingQueue>();
builder.Services.AddHostedService<CallProcessingWorker>();

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
    .AddCheck<GroqHealthCheck>("groq", tags: ["providers"])
    .AddCheck<StorageHealthCheck>("storage", tags: ["providers"]);

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

// The demo manager account — the one account that can sign in.
await DemoUserSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);

// ---------- HTTP pipeline (order matters) ----------

app.UseForwardedHeaders(); // first: everything after sees the real visitor IP

// Unhandled exceptions -> 500 ProblemDetails; empty error responses (404, 405...) -> ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous(); // GET /openapi/v1.json
}

// No UseHttpsRedirection: locally we use plain http, and on Render HTTPS is
// handled by Render's proxy in front of the container.

app.UseCors();           // before auth, so browser preflight requests (no token) are answered
app.UseRateLimiter();
app.UseAuthentication(); // who is it? (reads and verifies the token)
app.UseAuthorization();  // may they? (the fallback policy requires sign-in)

app.MapControllers();
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
}).AllowAnonymous(); // Render's health check has no token
app.MapHealthChecks("/api/health/providers", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("providers"),
    ResponseWriter = HealthResponseWriter.WriteJson,
}).AllowAnonymous();

app.Run();

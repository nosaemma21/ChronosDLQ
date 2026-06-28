using System.Text.Json;
using ChronosDLQ.App.Data;
using ChronosDLQ.app.Health;
using ChronosDLQ.App.Health;
using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

var chronosDbConnectionString =
    builder.Configuration.GetConnectionString("ChronosDb")
    ?? throw new InvalidOperationException("ChronosDb connection string is not configured");

builder.Services.AddDbContextFactory<ChronosDbContext>(options =>
{
    options.UseNpgsql(chronosDbConnectionString);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ChronosUiPolicy",
        policy =>
        {
            // policy.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader();
            if (builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
            {
                // vite def
                policy.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader();
                return;
            }

            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        }
    );
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365); //one year access
    options.IncludeSubDomains = true;
    options.Preload = true; //browser preload availability
});

//Setting up serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:l}{NewLine}{Exception}"
    )
    .CreateLogger();

// Using serilog
builder.Host.UseSerilog();

builder.Services.AddControllers().AddNewtonsoftJson();

// adding healthchecks
builder
    .Services.AddHealthChecks()
    .AddCheck(
        "api",
        () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
    )
    .AddCheck<PostgresHealthCheck>("postgres")
    .AddCheck<RabbitMqAmqHealthCheck>("rabbitmq-amqp")
    .AddCheck<RabbitMqManagementHealthCheck>("rabbitmq-management");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddSingleton<IMessageIndexStore, MessageIndexStore>();
builder.Services.AddSingleton<IMessageIndexStore, PostgresMessageIndexStore>();
builder.Services.AddSingleton<
    IRabbitMqConnectionSettingsProvider,
    RabbitMqConnectionSettingsProvider
>();

// Registering RabbitMQ low level core consumer engine
builder.Services.AddSingleton<IMessageBrokerConsumer, RabbitMqConsumer>();
builder.Services.AddSingleton<IQueueWatchService, QueueWatchService>();
builder.Services.AddHttpClient<IQueueDiscoveryService, RabbitMqManagementQueueDiscovery>();

// Registering ops business engine for replaying corrected payloads
builder.Services.AddScoped<IMessageReplayService, MessageReplayService>();

var app = builder.Build();

var chronosApiKey = builder.Configuration["Chronos:ApiKey"];
var chronosOpertorKey = builder.Configuration["Chronos:OperatorKey"];

//---------- API AUTH --------------
// will throw when non-dev env has no key ❌
if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(chronosApiKey))
{
    throw new InvalidOperationException(
        "Chronos API key must be configured outside dev environment"
    );
}

if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(chronosOpertorKey))
{
    throw new InvalidOperationException(
        "Chronos API key must be configured outside dev environment"
    );
}

app.Use(
    async (cxt, next) =>
    {
        // for preflight requests
        if (cxt.Request.Method == HttpMethods.Options)
        {
            await next();
            return;
        }

        //had to allow allow this run pass the auth
        if (cxt.Request.Path.StartsWithSegments("/api/health"))
        {
            await next();
            return;
        }

        if (app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(chronosApiKey))
        {
            await next();
            return;
        }

        var providedApiKey = cxt.Request.Headers["X-CHRONOS-API-KEY"].FirstOrDefault();

        if (!string.Equals(providedApiKey, chronosApiKey, StringComparison.Ordinal))
        {
            cxt.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await cxt.Response.WriteAsJsonAsync(new { message = "Unauthorized request" });
            return;
        }

        var isReplayRequest =
            cxt.Request.Method == HttpMethods.Post
            && cxt.Request.Path.StartsWithSegments("/api/messages/replay");

        var isDiscardRequest =
            cxt.Request.Method == HttpMethods.Delete
            && cxt.Request.Path.StartsWithSegments("/api/messages");

        var isRabbitMqConfigurationWrite =
            cxt.Request.Method == HttpMethods.Put
            && cxt.Request.Path.StartsWithSegments("/api/rabbitmq/configuration");

        var requiresOperatorPermission =
            isReplayRequest || isDiscardRequest || isRabbitMqConfigurationWrite;

        if (requiresOperatorPermission)
        {
            var providedOperatorKey = cxt
                .Request.Headers["X-CHRONOS-OPERATOR-KEY"]
                .FirstOrDefault();

            if (!string.Equals(providedOperatorKey, chronosOpertorKey, StringComparison.Ordinal))
            {
                cxt.Response.StatusCode = StatusCodes.Status403Forbidden;
                await cxt.Response.WriteAsJsonAsync(
                    new { message = "Chronos Op permission required" }
                );
                return;
            }
        }

        await next();
    }
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors("ChronosUiPolicy");
app.UseAuthorization();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

//healthcheck
app.MapHealthChecks(
    "/api/health",
    new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                }),
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        },
    }
);

app.MapControllers();

try
{
    Log.Information("Starting ChronosDLQ host...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

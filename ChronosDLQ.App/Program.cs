using ChronosDLQ.App.Data;
using ChronosDLQ.App.Services;
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddSingleton<IMessageIndexStore, MessageIndexStore>();
builder.Services.AddSingleton<IMessageIndexStore, PostgresMessageIndexStore>();

// Registering RabbitMQ low level core consumer engine
builder.Services.AddSingleton<IMessageBrokerConsumer, RabbitMqConsumer>();
builder.Services.AddSingleton<IQueueWatchService, QueueWatchService>();
builder.Services.AddHttpClient<IQueueDiscoveryService, RabbitMqManagementQueueDiscovery>();

// Registering ops business engine for replaying corrected payloads
builder.Services.AddScoped<IMessageReplayService, MessageReplayService>();

var app = builder.Build();

var chronosApiKey = builder.Configuration["Chronos:ApiKey"];
var chronosOpertorKey = builder.Configuration["Chronos:OperatorKey"];

var rabbitMqUserName = builder.Configuration["RabbitMq:UserName"];
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"];

var rabbitMqManagementBaseUrl = builder.Configuration["RabbitMq:ManagementBaseUrl"];

if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(rabbitMqManagementBaseUrl))
{
    throw new InvalidOperationException(
        "RabbitMQ Management API base URL must be configured outside Development."
    );
}

// ------------ MANAGER AUTH SECURITY -----------
if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(rabbitMqUserName))
{
    throw new InvalidOperationException("RabbitMQ username must be configured outside Dev env.");
}

if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(rabbitMqPassword))
{
    throw new InvalidOperationException("RabbitMQ password must be configured outside Dev env.");
}

if (
    !app.Environment.IsDevelopment()
    && string.Equals(rabbitMqUserName, "guest", StringComparison.Ordinal)
)
{
    throw new InvalidOperationException("No RabbitMQ guest user outside dev env");
}

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

        var requiresOperatorPermission = isReplayRequest || isDiscardRequest;

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

using ChronosDLQ.App.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ChronosUiPolicy",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader();
        }
    );
});

//Setting up serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:l}{NewLine}{Exception}"
    )
    .CreateLogger();

// Using serilog as our default logger
builder.Host.UseSerilog();

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMessageIndexStore, MessageIndexStore>();

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

if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(rabbitMqUserName))
{
    throw new InvalidOperationException("RabbitMQ username must be configured outside Dev env.");
}

if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(rabbitMqPassword))
{
    throw new InvalidOperationException("RabbitMQ password must be configured outside Dev env.");
}

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

app.UseHttpsRedirection();
app.UseCors("ChronosUiPolicy");
app.UseAuthorization();
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

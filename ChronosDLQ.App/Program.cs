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

// Registering ops business engine for replaying corrected payloads
builder.Services.AddScoped<IMessageReplayService, MessageReplayService>();

// Continuous long running bg service
builder.Services.AddHostedService<QueueConsumerWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ChronosUiPolicy");
app.UseAuthorization();
app.UseHttpsRedirection();
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

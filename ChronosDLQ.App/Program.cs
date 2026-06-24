using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//Setting up serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:l}{NewLine}{Exception}"
    )
    .CreateLogger();

// Using serilog as our default logger
builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMessageIndexStore, MessageIndexStore>();

// Registering RabbitMQ low level core consumer engine
builder.Services.AddSingleton<IMessageBrokerConsumer, RabbitMqConsumer>();

// Continuous long running bg service
builder.Services.AddHostedService<QueueConsumerWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

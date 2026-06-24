using ChronosDLQ.App.Services;
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

builder.Services.AddSingleton<IMessageIndexStore, MessageIndexStore>();

var app = builder.Build();

app.UseAuthorization();
app.UseHttpsRedirection();

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

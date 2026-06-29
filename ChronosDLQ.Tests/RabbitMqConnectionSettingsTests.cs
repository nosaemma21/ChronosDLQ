using ChronosDLQ.App.Services;
using Microsoft.Extensions.Configuration;

namespace ChronosDLQ.Tests;

public class RabbitMqConnectionSettingsTests
{
    [Fact]
    public void FromConfiguration_ShouldParseConnectionUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RabbitMq:ConnectionUrl"] =
                        "amqps://chronos-user:chronos-pass@lemur.rmq.cloudamqp.com/chronos-vhost",
                }
            )
            .Build();

        var settings = RabbitMqConnectionSettings.FromConfiguration(configuration);

        Assert.Equal("lemur.rmq.cloudamqp.com", settings.HostName);
        Assert.Equal("chronos-user", settings.UserName);
        Assert.Equal("chronos-pass", settings.Password);
        Assert.Equal("chronos-vhost", settings.VirtualHost);
        Assert.Equal("https://lemur.rmq.cloudamqp.com", settings.ManagementBaseUrl);
    }

    [Fact]
    public void FromConfiguration_ShouldLetExplicitManagementUrlWin()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RabbitMq:ConnectionUrl"] =
                        "amqps://chronos-user:chronos-pass@lemur.rmq.cloudamqp.com/chronos-vhost",
                    ["RabbitMq:ManagementBaseUrl"] = "https://custom-management.example.com",
                }
            )
            .Build();

        var settings = RabbitMqConnectionSettings.FromConfiguration(configuration);

        Assert.Equal("https://custom-management.example.com", settings.ManagementBaseUrl);
    }

    [Fact]
    public void FromConfiguration_ShouldInferLocalManagementPortFromAmqpPort()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RabbitMq:ConnectionUrl"] = "amqp://localhost:5672",
                }
            )
            .Build();

        var settings = RabbitMqConnectionSettings.FromConfiguration(configuration);

        Assert.Equal("localhost", settings.HostName);
        Assert.Equal("guest", settings.UserName);
        Assert.Equal("guest", settings.Password);
        Assert.Equal("/", settings.VirtualHost);
        Assert.Equal("http://localhost:15672", settings.ManagementBaseUrl);
    }

    [Fact]
    public void FromConfiguration_ShouldApplyLocalhostAlias()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RabbitMq:ConnectionUrl"] = "amqp://localhost:5672",
                    ["RabbitMq:LocalhostAlias"] = "chronos-rabbitmq",
                }
            )
            .Build();

        var settings = RabbitMqConnectionSettings.FromConfiguration(configuration);

        Assert.Equal("chronos-rabbitmq", settings.HostName);
        Assert.Equal("http://chronos-rabbitmq:15672", settings.ManagementBaseUrl);
    }
}

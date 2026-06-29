using RabbitMQ.Client;

namespace ChronosDLQ.App.Services;

public sealed class RabbitMqConnectionSettings
{
    private RabbitMqConnectionSettings(
        string? connectionUrl,
        string hostName,
        int? port,
        string userName,
        string password,
        string virtualHost,
        string? managementBaseUrl
    )
    {
        ConnectionUrl = connectionUrl;
        HostName = hostName;
        Port = port;
        UserName = userName;
        Password = password;
        VirtualHost = virtualHost;
        ManagementBaseUrl = managementBaseUrl;
    }

    public string? ConnectionUrl { get; }
    public string HostName { get; }
    public int? Port { get; }
    public string UserName { get; }
    public string Password { get; }
    public string VirtualHost { get; }
    public string? ManagementBaseUrl { get; }

    public static bool HasConnectionUrlConfiguration(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration["RabbitMq:ConnectionUrl"])
            || !string.IsNullOrWhiteSpace(configuration["RabbitMq:Url"])
            || !string.IsNullOrWhiteSpace(configuration["RabbitMq:Uri"]);
    }

    public static bool HasConfiguration(IConfiguration configuration)
    {
        return HasConnectionUrlConfiguration(configuration)
            || !string.IsNullOrWhiteSpace(configuration["RabbitMq:HostName"]);
    }

    public static RabbitMqConnectionSettings FromConfiguration(IConfiguration configuration)
    {
        var connectionUrl =
            configuration["RabbitMq:ConnectionUrl"]
            ?? configuration["RabbitMq:Url"]
            ?? configuration["RabbitMq:Uri"];

        if (!string.IsNullOrWhiteSpace(connectionUrl))
        {
            return FromConnectionUrl(connectionUrl, configuration["RabbitMq:ManagementBaseUrl"]);
        }

        return new RabbitMqConnectionSettings(
            connectionUrl: null,
            hostName: configuration["RabbitMq:HostName"] ?? "localhost",
            port: null,
            userName: configuration["RabbitMq:UserName"] ?? "guest",
            password: configuration["RabbitMq:Password"] ?? "guest",
            virtualHost: configuration["RabbitMq:VirtualHost"] ?? "/",
            managementBaseUrl: configuration["RabbitMq:ManagementBaseUrl"]
        );
    }

    public ConnectionFactory CreateConnectionFactory(bool automaticRecoveryEnabled = false)
    {
        var factory = !string.IsNullOrWhiteSpace(ConnectionUrl)
            ? new ConnectionFactory { Uri = new Uri(ConnectionUrl) }
            : new ConnectionFactory
            {
                HostName = HostName,
                UserName = UserName,
                Password = Password,
                VirtualHost = VirtualHost,
            };

        if (Port.HasValue)
        {
            factory.Port = Port.Value;
        }

        factory.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
        factory.TopologyRecoveryEnabled = automaticRecoveryEnabled;
        factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

        return factory;
    }

    public static RabbitMqConnectionSettings FromConnectionUrl(
        string connectionUrl,
        string? configuredManagementBaseUrl
    )
    {
        if (!Uri.TryCreate(connectionUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("RabbitMQ connection URL is not a valid URI.");
        }

        if (uri.Scheme is not ("amqp" or "amqps"))
        {
            throw new InvalidOperationException("RabbitMQ connection URL must use amqp or amqps.");
        }

        var (userName, password) = ParseCredentials(uri.UserInfo);
        var virtualHost = ParseVirtualHost(uri);

        return new RabbitMqConnectionSettings(
            connectionUrl,
            uri.Host,
            uri.IsDefaultPort ? null : uri.Port,
            userName,
            password,
            virtualHost,
            configuredManagementBaseUrl ?? InferManagementBaseUrl(uri)
        );
    }

    private static (string UserName, string Password) ParseCredentials(string userInfo)
    {
        if (string.IsNullOrWhiteSpace(userInfo))
        {
            return ("guest", "guest");
        }

        var separatorIndex = userInfo.IndexOf(':');
        if (separatorIndex < 0)
        {
            return (Uri.UnescapeDataString(userInfo), string.Empty);
        }

        return (
            Uri.UnescapeDataString(userInfo[..separatorIndex]),
            Uri.UnescapeDataString(userInfo[(separatorIndex + 1)..])
        );
    }

    private static string ParseVirtualHost(Uri uri)
    {
        var trimmedPath = uri.AbsolutePath.Trim('/');
        return string.IsNullOrWhiteSpace(trimmedPath)
            ? "/"
            : Uri.UnescapeDataString(trimmedPath);
    }

    private static string InferManagementBaseUrl(Uri uri)
    {
        var scheme = uri.Scheme == "amqps" ? "https" : "http";
        int? port = null;

        if (uri.Scheme == "amqp" && (uri.IsDefaultPort || uri.Port == 5672))
        {
            port = 15672;
        }
        else if (!uri.IsDefaultPort)
        {
            port = uri.Port;
        }

        return port.HasValue ? $"{scheme}://{uri.Host}:{port.Value}" : $"{scheme}://{uri.Host}";
    }
}

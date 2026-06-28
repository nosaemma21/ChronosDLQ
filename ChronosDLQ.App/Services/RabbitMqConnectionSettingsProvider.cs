using ChronosDLQ.App.Data;
using ChronosDLQ.App.Models;
using Microsoft.EntityFrameworkCore;

namespace ChronosDLQ.App.Services;

public class RabbitMqConnectionSettingsProvider : IRabbitMqConnectionSettingsProvider
{
    private const int RuntimeConfigurationId = 1;

    private readonly IDbContextFactory<ChronosDbContext> _dbContextFactory;
    private readonly IConfiguration _configuration;

    public RabbitMqConnectionSettingsProvider(
        IDbContextFactory<ChronosDbContext> dbContextFactory,
        IConfiguration configuration
    )
    {
        _dbContextFactory = dbContextFactory;
        _configuration = configuration;
    }

    public async Task<RabbitMqConnectionSettings?> GetSettingsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (RabbitMqConnectionSettings.HasConnectionUrlConfiguration(_configuration))
        {
            return RabbitMqConnectionSettings.FromConfiguration(_configuration);
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(
            cancellationToken
        );
        var runtimeConfiguration = await dbContext.RabbitMqRuntimeConfigurations.FindAsync(
            [RuntimeConfigurationId],
            cancellationToken
        );

        if (runtimeConfiguration is not null)
        {
            return RabbitMqConnectionSettings.FromConnectionUrl(
                runtimeConfiguration.ConnectionUrl,
                runtimeConfiguration.ManagementBaseUrl
            );
        }

        return RabbitMqConnectionSettings.HasConfiguration(_configuration)
            ? RabbitMqConnectionSettings.FromConfiguration(_configuration)
            : null;
    }

    public async Task<RabbitMqConnectionSettings> SaveAsync(
        string connectionUrl,
        string? managementBaseUrl,
        CancellationToken cancellationToken = default
    )
    {
        var settings = RabbitMqConnectionSettings.FromConnectionUrl(
            connectionUrl,
            managementBaseUrl
        );

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(
            cancellationToken
        );
        var runtimeConfiguration = await dbContext.RabbitMqRuntimeConfigurations.FindAsync(
            [RuntimeConfigurationId],
            cancellationToken
        );

        if (runtimeConfiguration is null)
        {
            runtimeConfiguration = new RabbitMqRuntimeConfiguration
            {
                Id = RuntimeConfigurationId,
            };
            dbContext.RabbitMqRuntimeConfigurations.Add(runtimeConfiguration);
        }

        runtimeConfiguration.ConnectionUrl = connectionUrl.Trim();
        runtimeConfiguration.ManagementBaseUrl = string.IsNullOrWhiteSpace(managementBaseUrl)
            ? null
            : managementBaseUrl.Trim();
        runtimeConfiguration.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }
}

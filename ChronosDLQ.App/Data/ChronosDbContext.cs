using System.Text.Json;
using ChronosDLQ.App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ChronosDLQ.App.Data;

public class ChronosDbContext : DbContext
{
    public ChronosDbContext(DbContextOptions<ChronosDbContext> options)
        : base(options) { }

    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<RabbitMqRuntimeConfiguration> RabbitMqRuntimeConfigurations =>
        Set<RabbitMqRuntimeConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //   base.OnModelCreating(modelBuilder);
        var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (left, right) =>
                JsonSerializer.Serialize(left, JsonSerializerOptions.Default)
                == JsonSerializer.Serialize(right, JsonSerializerOptions.Default),
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default).GetHashCode(),
            value =>
                JsonSerializer.Deserialize<Dictionary<string, string>>(
                    JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
                    JsonSerializerOptions.Default
                ) ?? new Dictionary<string, string>()
        );

        modelBuilder.Entity<DeadLetterMessage>(entity =>
        {
            entity.HasKey(message => message.MessageId);

            entity.Property(message => message.QueueName).IsRequired();
            entity.Property(message => message.QueueName).IsRequired();
            entity.Property(message => message.RawPayload).IsRequired();
            entity.Property(message => message.ExceptionMessage).IsRequired();

            entity.Property(message => message.RawPayload).HasColumnType("text");
            entity.Property(message => message.ExceptionMessage).HasColumnType("text");

            entity
                .Property(message => message.Metadata)
                .HasColumnType("jsonb")
                .HasConversion(
                    value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
                    value =>
                        JsonSerializer.Deserialize<Dictionary<string, string>>(
                            value,
                            JsonSerializerOptions.Default
                        ) ?? new Dictionary<string, string>()
                )
                .Metadata.SetValueComparer(dictionaryComparer);

            entity
                .Property(message => message.Headers)
                .HasColumnType("jsonb")
                .HasConversion(
                    value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
                    value =>
                        JsonSerializer.Deserialize<Dictionary<string, string>>(
                            value,
                            JsonSerializerOptions.Default
                        ) ?? new Dictionary<string, string>()
                )
                .Metadata.SetValueComparer(dictionaryComparer);

            entity.HasIndex(message => message.QueueName);
            entity.HasIndex(message => message.Timestamp);
        });

        modelBuilder.Entity<RabbitMqRuntimeConfiguration>(entity =>
        {
            entity.HasKey(configuration => configuration.Id);
            entity.Property(configuration => configuration.ConnectionUrl).IsRequired();
            entity.Property(configuration => configuration.ConnectionUrl).HasColumnType("text");
            entity.Property(configuration => configuration.ManagementBaseUrl).HasColumnType("text");
        });
    }
}

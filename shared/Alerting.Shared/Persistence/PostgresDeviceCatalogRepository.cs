using Alerting.Shared.Configuration;
using Alerting.Shared.Enums;
using Alerting.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Alerting.Shared.Persistence;

public sealed class PostgresDeviceCatalogRepository : IDeviceCatalogRepository, IAsyncDisposable
{
    private readonly PostgresOptions _options;
    private readonly ILogger<PostgresDeviceCatalogRepository> _logger;
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _qualifiedTableName;

    public PostgresDeviceCatalogRepository(
        IOptions<PostgresOptions> options,
        ILogger<PostgresDeviceCatalogRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
        _dataSource = NpgsqlDataSource.Create(_options.ConnectionString);
        _qualifiedTableName = $"{QuoteIdentifier(_options.Schema)}.{QuoteIdentifier(_options.DevicesTable)}";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var schemaSql = $"create schema if not exists {QuoteIdentifier(_options.Schema)};";
        await using (var schemaCommand = new NpgsqlCommand(schemaSql, connection))
        {
            await schemaCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var tableSql = $"""
            create table if not exists {_qualifiedTableName} (
                device_id text primary key,
                user_id text not null,
                channels text[] not null default ARRAY[]::text[],
                push_token text null,
                is_online boolean not null,
                network_type text not null,
                last_updated_utc timestamptz not null
            );
            """;

        await using (var tableCommand = new NpgsqlCommand(tableSql, connection))
        {
            await tableCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var indexSql = $"""
            create index if not exists ix_{_options.DevicesTable}_user_id
            on {_qualifiedTableName} (user_id);
            """;

        await using var indexCommand = new NpgsqlCommand(indexSql, connection);
        await indexCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("PostgreSQL device catalog initialized at {TableName}", _qualifiedTableName);
    }

    public async Task<IReadOnlyCollection<RegisteredDevice>> GetAllAsync(CancellationToken cancellationToken)
    {
        var devices = new List<RegisteredDevice>();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
             select device_id, user_id, channels, push_token, is_online, network_type, last_updated_utc
             from {_qualifiedTableName}
             order by user_id, device_id;
             """,
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            devices.Add(new RegisteredDevice(
                DeviceId: reader.GetString(0),
                UserId: reader.GetString(1),
                Channels: ParseChannels(reader.GetFieldValue<string[]>(2)),
                PushToken: reader.IsDBNull(3) ? null : reader.GetString(3),
                IsOnline: reader.GetBoolean(4),
                NetworkType: reader.GetString(5),
                LastUpdatedUtc: reader.GetFieldValue<DateTime>(6)));
        }

        return devices;
    }

    public async Task UpsertAsync(DeviceRegisteredEvent deviceEvent, CancellationToken cancellationToken)
    {
        var sql = $"""
            insert into {_qualifiedTableName} (
                device_id, user_id, channels, push_token, is_online, network_type, last_updated_utc
            )
            values (
                @device_id, @user_id, @channels, @push_token, @is_online, @network_type, @last_updated_utc
            )
            on conflict (device_id) do update set
                user_id = excluded.user_id,
                channels = excluded.channels,
                push_token = excluded.push_token,
                is_online = excluded.is_online,
                network_type = excluded.network_type,
                last_updated_utc = excluded.last_updated_utc;
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("device_id", deviceEvent.DeviceId);
        command.Parameters.AddWithValue("user_id", deviceEvent.UserId);
        command.Parameters.AddWithValue("channels", deviceEvent.Channels.Select(channel => channel.ToString()).ToArray());
        command.Parameters.AddWithValue("push_token", (object?)deviceEvent.PushToken ?? DBNull.Value);
        command.Parameters.AddWithValue("is_online", deviceEvent.IsOnline);
        command.Parameters.AddWithValue("network_type", deviceEvent.NetworkType);
        command.Parameters.AddWithValue("last_updated_utc", deviceEvent.TimestampUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(DeviceStatusUpdatedEvent statusEvent, CancellationToken cancellationToken)
    {
        var sql = $"""
            insert into {_qualifiedTableName} (
                device_id, user_id, channels, push_token, is_online, network_type, last_updated_utc
            )
            values (
                @device_id, @user_id, @channels, @push_token, @is_online, @network_type, @last_updated_utc
            )
            on conflict (device_id) do update set
                is_online = excluded.is_online,
                network_type = excluded.network_type,
                last_updated_utc = excluded.last_updated_utc;
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("device_id", statusEvent.DeviceId);
        command.Parameters.AddWithValue("user_id", "unknown");
        command.Parameters.AddWithValue("channels", Array.Empty<string>());
        command.Parameters.AddWithValue("push_token", DBNull.Value);
        command.Parameters.AddWithValue("is_online", statusEvent.IsOnline);
        command.Parameters.AddWithValue("network_type", statusEvent.NetworkType);
        command.Parameters.AddWithValue("last_updated_utc", statusEvent.TimestampUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();

    private static IReadOnlyCollection<DispatchChannel> ParseChannels(IEnumerable<string> values) =>
        values
            .Select(value => Enum.TryParse<DispatchChannel>(value, ignoreCase: true, out var channel)
                ? channel
                : (DispatchChannel?)null)
            .Where(channel => channel.HasValue)
            .Select(channel => channel!.Value)
            .ToArray();

    private static string QuoteIdentifier(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"") + "\"";
}

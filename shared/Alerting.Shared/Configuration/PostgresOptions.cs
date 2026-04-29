namespace Alerting.Shared.Configuration;

public sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=alerting;Username=postgres;Password=postgres";
    public string Schema { get; set; } = "public";
    public string DevicesTable { get; set; } = "devices";
}

// Trách nhiệm: đọc computer_code và connection config của từng máy station.
// Credential chỉ đến từ environment hoặc station.json; không hard-code trong source.

using System.Text.Json;

namespace NetCafe.Client;

public sealed class StationConfig
{
    public string ComputerCode { get; init; } = "PC001";
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = "netcafe_qa";
    public string Username { get; init; } = "station_runtime";
    public string Password { get; init; } = string.Empty;
    public string SslMode { get; init; } = "Disable";
    public int HeartbeatSeconds { get; init; } = 30;

    public static StationConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "station.json");
        var fileConfig = File.Exists(path)
            ? JsonSerializer.Deserialize<StationConfig>(File.ReadAllText(path))
            : null;
        var config = fileConfig ?? new StationConfig();
        return new StationConfig
        {
            ComputerCode = Environment.GetEnvironmentVariable("NETCAFE_COMPUTER_CODE") ?? config.ComputerCode,
            Host = Environment.GetEnvironmentVariable("DB_HOST") ?? config.Host,
            Port = int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var port) ? port : config.Port,
            Database = Environment.GetEnvironmentVariable("DB_NAME") ?? config.Database,
            Username = Environment.GetEnvironmentVariable("STATION_DB_USER") ?? config.Username,
            Password = Environment.GetEnvironmentVariable("STATION_DB_PASSWORD") ?? config.Password,
            SslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? config.SslMode,
            HeartbeatSeconds = int.TryParse(Environment.GetEnvironmentVariable("STATION_HEARTBEAT_SECONDS"), out var heartbeat)
                ? Math.Max(5, heartbeat)
                : config.HeartbeatSeconds
        };
    }

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};" +
        $"SSL Mode={SslMode};Include Error Detail=true;";
}

// Trách nhiệm: đọc computer_code và connection config của từng máy station.
// Credential chỉ đến từ environment, .env cục bộ hoặc station.json; không hard-code trong source.

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
        var localEnv = ReadDotEnv();
        return new StationConfig
        {
            ComputerCode = GetSetting(localEnv, "NETCAFE_COMPUTER_CODE") ?? config.ComputerCode,
            Host = GetSetting(localEnv, "DB_HOST") ?? config.Host,
            Port = int.TryParse(GetSetting(localEnv, "DB_PORT"), out var port) ? port : config.Port,
            Database = GetSetting(localEnv, "DB_NAME") ?? config.Database,
            Username = GetSetting(localEnv, "STATION_DB_USER") ?? config.Username,
            Password = GetSetting(localEnv, "STATION_DB_PASSWORD") ?? config.Password,
            SslMode = GetSetting(localEnv, "DB_SSL_MODE") ?? config.SslMode,
            HeartbeatSeconds = int.TryParse(GetSetting(localEnv, "STATION_HEARTBEAT_SECONDS"), out var heartbeat)
                ? Math.Max(5, heartbeat)
                : config.HeartbeatSeconds
        };
    }

    private static string? GetSetting(IReadOnlyDictionary<string, string> localEnv, string key)
    {
        var processValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(processValue))
        {
            return processValue.Trim();
        }

        return localEnv.TryGetValue(key, out var fileValue) && !string.IsNullOrWhiteSpace(fileValue)
            ? fileValue.Trim()
            : null;
    }

    private static IReadOnlyDictionary<string, string> ReadDotEnv()
    {
        var explicitPath = Environment.GetEnvironmentVariable("NETCAFE_ENV_FILE");
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            candidates.Add(explicitPath);
        }

        candidates.AddRange(EnumerateParentPaths(Environment.CurrentDirectory));
        candidates.AddRange(EnumerateParentPaths(AppContext.BaseDirectory));
        var envPath = candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(File.Exists);
        if (envPath is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#') || !trimmed.Contains('='))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim().Trim('"').Trim('\'');
            values[key] = value;
        }

        return values;
    }

    private static IEnumerable<string> EnumerateParentPaths(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            yield return Path.Combine(directory.FullName, ".env");
            directory = directory.Parent;
        }
    }

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};" +
        $"SSL Mode={SslMode};Include Error Detail=true;";
}

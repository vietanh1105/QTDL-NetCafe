using System;
using System.IO;

namespace NetCafe.App
{
    public static class AppConfig
    {
        public static string Host { get; set; } = "127.0.0.1";
        public static int Port { get; set; } = 5432;
        public static string Database { get; set; } = "netcafe_db";
        public static string Username { get; set; } = "postgres";
        public static string Password { get; set; } = "vietanh1105@";

        public static void LoadEnv()
        {
            var searchPaths = new[]
            {
                ".env",
                "../.env",
                "../../.env",
                "../../../.env",
                "../../../../.env"
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        var parts = trimmed.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var val = parts[1].Trim();
                            if (key == "DB_HOST") Host = val;
                            else if (key == "DB_PORT" && int.TryParse(val, out var p)) Port = p;
                            else if (key == "DB_NAME") Database = val;
                            else if (key == "DB_USER") Username = val;
                            else if (key == "DB_PASSWORD") Password = val;
                        }
                    }
                    break;
                }
            }
        }

        public static string GetConnectionString(string? user = null, string? pwd = null)
        {
            var u = user ?? Username;
            var p = pwd ?? Password;
            return $"Host={Host};Port={Port};Database={Database};Username={u};Password={p};Include Error Detail=true;";
        }
    }
}

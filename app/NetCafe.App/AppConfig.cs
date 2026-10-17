// Trách nhiệm: đọc cấu hình connection cho Admin từ environment hoặc .env.
// Không chứa credential thật; role/password phải được cấp ngoài source control.

using System;
using System.IO;

namespace NetCafe.App
{
    public static class AppConfig
    {
        public static string Host { get; set; } = "localhost";
        public static int Port { get; set; } = 5432;
        public static string Database { get; set; } = "netcafe_qa";
        public static string Username { get; set; } = "postgres";
        public static string Password { get; set; } = string.Empty;
        public static string SslMode { get; set; } = "Disable";
        public static bool TrustServerCertificate { get; set; } = true;

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
                            else if (key == "DB_SSL_MODE") SslMode = val;
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
            var ssl = !string.IsNullOrEmpty(SslMode) ? $";SSL Mode={SslMode};Trust Server Certificate={TrustServerCertificate}" : "";
            return $"Host={Host};Port={Port};Database={Database};Username={u};Password={p};Include Error Detail=true{ssl};";
        }

        public static string GetConfiguredRolePassword(string role)
        {
            var key = role switch
            {
                "nv_thao" => "DB_CASHIER_PASSWORD",
                "nv_manager" => "DB_MANAGER_PASSWORD",
                "nv_hung" => "DB_TECHNICIAN_PASSWORD",
                _ => "DB_ADMIN_PASSWORD"
            };
            return Environment.GetEnvironmentVariable(key) ?? string.Empty;
        }
    }
}

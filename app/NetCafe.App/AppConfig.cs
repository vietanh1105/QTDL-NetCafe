// Trách nhiệm: đọc cấu hình connection cho Admin từ environment hoặc .env.
// Không chứa credential thật; role/password phải được cấp ngoài source control.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Npgsql;

namespace NetCafe.App
{
    public static class AppConfig
    {
        public static string Host { get; set; } = "localhost";
        public static int Port { get; set; } = 5432;
        public static string Database { get; set; } = "netcafe_qa";
        public static string Username { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;
        public static string SslMode { get; set; } = "Disable";
        public static bool TrustServerCertificate { get; set; } = true;
        public static bool IncludeErrorDetail { get; private set; }
        public static string AdminUser { get; private set; } = "nv_admin";
        public static string ManagerUser { get; private set; } = "nv_manager";
        public static string CashierUser { get; private set; } = "nv_thao";
        public static string AdminPassword { get; private set; } = string.Empty;
        public static string ManagerPassword { get; private set; } = string.Empty;
        public static string CashierPassword { get; private set; } = string.Empty;
        public static int StationServiceEmployeeId { get; private set; }
        public static int AdminEmployeeId { get; private set; }
        public static string LoadedEnvFile { get; private set; } = string.Empty;

        public static void LoadEnv()
        {
            var explicitPath = Environment.GetEnvironmentVariable("NETCAFE_ENV_FILE");
            var candidateFileNames = new[] { ".env", ".env.example" };

            foreach (var fileName in candidateFileNames)
            {
                var searchPaths = new List<string>();
                if (!string.IsNullOrWhiteSpace(explicitPath))
                {
                    searchPaths.Add(Path.GetFullPath(explicitPath));
                }
                else
                {
                    var roots = new[] { Environment.CurrentDirectory, AppContext.BaseDirectory };
                    foreach (var root in roots)
                    {
                        var directory = new DirectoryInfo(root);
                        for (var depth = 0; depth < 7 && directory is not null; depth++, directory = directory.Parent)
                        {
                            searchPaths.Add(Path.Combine(directory.FullName, fileName));
                        }
                    }
                }

                foreach (var path in searchPaths.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (File.Exists(path))
                    {
                        LoadFromFile(path);
                        LoadedEnvFile = Path.GetFullPath(path);
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(LoadedEnvFile))
                {
                    break;
                }
            }

            Host = Environment.GetEnvironmentVariable("DB_HOST") ?? Host;
            if (int.TryParse(Environment.GetEnvironmentVariable("DB_PORT"), out var envPort)) Port = envPort;
            Database = Environment.GetEnvironmentVariable("DB_NAME") ?? Database;
            Username = Environment.GetEnvironmentVariable("DB_USER") ?? Username;
            Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Password;
            SslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? SslMode;
            AdminUser = Environment.GetEnvironmentVariable("DB_ADMIN_USER") ?? AdminUser;
            ManagerUser = Environment.GetEnvironmentVariable("DB_MANAGER_USER") ?? ManagerUser;
            CashierUser = Environment.GetEnvironmentVariable("DB_CASHIER_USER") ?? CashierUser;
            AdminPassword = Environment.GetEnvironmentVariable("DB_ADMIN_PASSWORD") ?? AdminPassword;
            ManagerPassword = Environment.GetEnvironmentVariable("DB_MANAGER_PASSWORD") ?? ManagerPassword;
            CashierPassword = Environment.GetEnvironmentVariable("DB_CASHIER_PASSWORD") ?? CashierPassword;
            IncludeErrorDetail = bool.TryParse(
                Environment.GetEnvironmentVariable("DB_INCLUDE_ERROR_DETAIL"),
                out var includeErrorDetail) && includeErrorDetail;
            if (string.IsNullOrWhiteSpace(Username))
            {
                Username = AdminUser;
            }
            if (string.IsNullOrWhiteSpace(Password) &&
                string.Equals(Username, AdminUser, StringComparison.OrdinalIgnoreCase))
            {
                Password = AdminPassword;
            }
            if (int.TryParse(Environment.GetEnvironmentVariable("STATION_SERVICE_EMPLOYEE_ID"), out var envEmployeeId))
            {
                StationServiceEmployeeId = envEmployeeId;
            }
            if (int.TryParse(Environment.GetEnvironmentVariable("DB_ADMIN_EMPLOYEE_ID"), out var envAdminEmployeeId))
            {
                AdminEmployeeId = envAdminEmployeeId;
            }
        }

        private static void LoadFromFile(string path)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                var parts = trimmed.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var val = parts[1].Trim().Trim('"').Trim('\'');

                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }

                    if (key == "DB_HOST") Host = val;
                    else if (key == "DB_PORT" && int.TryParse(val, out var p)) Port = p;
                    else if (key == "DB_NAME") Database = val;
                    else if (key == "DB_USER") Username = val;
                    else if (key == "DB_PASSWORD") Password = val;
                    else if (key == "DB_SSL_MODE") SslMode = val;
                    else if (key == "DB_ADMIN_USER") AdminUser = val;
                    else if (key == "DB_MANAGER_USER") ManagerUser = val;
                    else if (key == "DB_CASHIER_USER") CashierUser = val;
                    else if (key == "DB_ADMIN_PASSWORD") AdminPassword = val;
                    else if (key == "DB_MANAGER_PASSWORD") ManagerPassword = val;
                    else if (key == "DB_CASHIER_PASSWORD") CashierPassword = val;
                    else if (key == "DB_INCLUDE_ERROR_DETAIL" && bool.TryParse(val, out var includeErrorDetail))
                    {
                        IncludeErrorDetail = includeErrorDetail;
                    }
                    else if (key == "STATION_SERVICE_EMPLOYEE_ID" && int.TryParse(val, out var employeeId))
                    {
                        StationServiceEmployeeId = employeeId;
                    }
                    else if (key == "DB_ADMIN_EMPLOYEE_ID" && int.TryParse(val, out var adminEmployeeId))
                    {
                        AdminEmployeeId = adminEmployeeId;
                    }
                }
            }
        }

        public static string ConnectionSummary(string? user = null)
        {
            var effectiveUser = user ?? Username;
            var source = string.IsNullOrWhiteSpace(LoadedEnvFile) ? "Mặc định / Biến môi trường" : LoadedEnvFile;
            return $"Máy chủ={Host}:{Port} | CSDL={Database} | Người dùng={effectiveUser} | SSL={SslMode} | Nguồn={source}";
        }

        public static string GetConnectionString(string? user = null, string? pwd = null)
        {
            var u = user ?? Username;
            var p = pwd ?? Password;
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port,
                Database = Database,
                Username = u,
                Password = p,
                Timeout = 30,
                CommandTimeout = 60,
                KeepAlive = 15,
                TcpKeepAlive = true,
                TcpKeepAliveTime = 30,
                TcpKeepAliveInterval = 5,
                ConnectionIdleLifetime = 45,
                ConnectionPruningInterval = 10,
                Pooling = true,
                MaxPoolSize = 25,
                IncludeErrorDetail = AppConfig.IncludeErrorDetail
            };

            if (!string.IsNullOrEmpty(SslMode) &&
                Enum.TryParse<SslMode>(SslMode, true, out var parsedSsl))
            {
                builder.SslMode = parsedSsl;
            }

            return builder.ConnectionString;
        }

        public static string ResolveRole(string user)
        {
            if (string.Equals(user, AdminUser, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user, "nv_admin", StringComparison.OrdinalIgnoreCase))
            {
                return "role_admin";
            }
            if (string.Equals(user, ManagerUser, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user, "nv_manager", StringComparison.OrdinalIgnoreCase))
            {
                return "role_manager";
            }
            if (string.Equals(user, CashierUser, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user, "nv_thao", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user, "nv_nam", StringComparison.OrdinalIgnoreCase))
            {
                return "role_cashier";
            }
            return "role_unknown";
        }

        public static string GetConfiguredRolePassword(string user)
        {
            if (string.Equals(user, Username, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(Password))
            {
                return Password;
            }

            var pwd = ResolveRole(user) switch
            {
                "role_admin" => AdminPassword,
                "role_manager" => ManagerPassword,
                "role_cashier" => CashierPassword,
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(pwd))
            {
                pwd = Environment.GetEnvironmentVariable("DEMO_PASSWORD") ?? Password;
            }
            return pwd;
        }

        public static IReadOnlyList<string> GetConfiguredRoles()
        {
            var roles = new List<string>();
            foreach (var roleUser in new[] { AdminUser, ManagerUser, CashierUser })
            {
                if (!string.IsNullOrWhiteSpace(roleUser) &&
                    !string.IsNullOrWhiteSpace(GetConfiguredRolePassword(roleUser)) &&
                    !roles.Contains(roleUser, StringComparer.OrdinalIgnoreCase))
                {
                    roles.Add(roleUser);
                }
            }

            if (!string.IsNullOrWhiteSpace(Username) &&
                !string.IsNullOrWhiteSpace(Password) &&
                ResolveRole(Username) != "role_unknown" &&
                !roles.Contains(Username, StringComparer.OrdinalIgnoreCase))
            {
                roles.Add(Username);
            }

            if (roles.Count == 0 && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
                roles.Add(Username);
            }

            return roles;
        }
    }
}

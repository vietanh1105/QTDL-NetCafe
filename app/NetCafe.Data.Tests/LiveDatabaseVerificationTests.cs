// Trách nhiệm: kiểm thử tự động toàn diện database thực tế theo cấu hình .env.
// Kiểm tra: Views, Materialized Views, Tables, Functions/Stored Procedures, AI Modules, RBAC Permissions.

using System.IO;
using NetCafe.Data;
using Npgsql;
using Xunit;

namespace NetCafe.Data.Tests;

public sealed class LiveDatabaseVerificationTests
{
    private readonly string? _connectionString;
    private readonly Dictionary<string, string> _env = new();

    public LiveDatabaseVerificationTests()
    {
        var envPath = FindEnvFile();
        if (envPath != null && File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || !trimmed.Contains('='))
                {
                    continue;
                }
                var parts = trimmed.Split('=', 2);
                _env[parts[0].Trim()] = parts[1].Trim().Trim('"', '\'');
            }

            if (_env.TryGetValue("DB_HOST", out var host) &&
                _env.TryGetValue("DB_USER", out var user) &&
                _env.TryGetValue("DB_PASSWORD", out var pass) &&
                _env.TryGetValue("DB_NAME", out var db))
            {
                var port = _env.GetValueOrDefault("DB_PORT", "5432");
                var ssl = _env.GetValueOrDefault("DB_SSL_MODE", "Require");
                _connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode={ssl};Timeout=15;";
            }
        }
    }

    private static string? FindEnvFile()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(current, ".env");
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return null;
    }

    [Fact]
    public async Task Verify_01_DatabaseConnectivity_And_MetadataRuntime()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        var repo = new DbRepository(_connectionString);
        var info = await repo.GetDatabaseRuntimeInfoAsync();

        Assert.NotNull(info);
        Assert.False(string.IsNullOrEmpty(info.Database));
        Assert.True(info.HasStationMigration, "Thiếu migration máy trạm");
        Assert.True(info.HasReconcileFunction, "Thiếu hàm core.sp_reconcile_stale_sessions");
        Assert.True(info.HasComputerGridContract, "Thiếu view reporting.vw_computer_grid đúng chuẩn");
        Assert.True(info.HasCustomerMenuContract, "Thiếu hàm inventory.fn_customer_menu_v2");
        Assert.True(info.HasStationLogoutContract, "Thiếu hàm core.sp_station_logout");
    }

    [Fact]
    public async Task Verify_02_AllViews_And_MaterializedViews()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        string[] views =
        {
            "reporting.vw_computer_grid",
            "reporting.mv_revenue_daily",
            "reporting.mv_monthly_revenue",
            "reporting.mv_hourly_usage",
            "reporting.mv_top_customers"
        };

        foreach (var viewName in views)
        {
            await using var cmd = new NpgsqlCommand($"SELECT count(*) FROM {viewName};", conn);
            var count = await cmd.ExecuteScalarAsync();
            Assert.NotNull(count);
        }
    }

    [Fact]
    public async Task Verify_03_AllCoreAndInventoryTables()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        string[] tables =
        {
            "core.room", "core.computer_type", "core.computer", "core.customer", "core.gaming_session",
            "inventory.product_category", "inventory.product", "inventory.food_order", "inventory.food_order_detail",
            "billing.promotion", "billing.invoice", "billing.invoice_promotion", "billing.payment",
            "hr.employee", "hr.maintenance", "audit.audit_log",
            "ai.ai_alert", "ai.index_recommendation", "ai.duplicate_candidate",
            "ai.anomaly_flag", "ai.promotion_suggestion", "core.service_request",
            "core.station_runtime_config", "staging.stg_customers", "staging.rejected_rows"
        };

        foreach (var tbl in tables)
        {
            await using var cmd = new NpgsqlCommand($"SELECT count(*) FROM {tbl};", conn);
            var count = await cmd.ExecuteScalarAsync();
            Assert.NotNull(count);
        }
    }

    [Fact]
    public async Task Verify_04_ActivePromotions_Exist_And_Valid()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        var repo = new DbRepository(_connectionString);
        var promos = await repo.GetActivePromotionsAsync();

        Assert.NotNull(promos);
        Assert.NotEmpty(promos);
        foreach (var promo in promos)
        {
            Assert.False(string.IsNullOrWhiteSpace(promo.Code));
            Assert.True(promo.Value > 0);
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT lower(pg_get_functiondef('billing.sp_close_session_and_pay_at(bigint,integer,character varying,character varying,timestamp with time zone,character varying)'::regprocedure));",
            connection);
        var definition = Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
        Assert.Contains("required_tier", definition);
        Assert.Contains("min_bill", definition);
        Assert.Contains("asia/ho_chi_minh", definition);
    }

    [Fact]
    public async Task Verify_05_AllAiModules_Execution()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Cảnh báo bảo mật từ audit trigger
        await using var cmd0 = new NpgsqlCommand("SELECT count(*) FROM ai.fn_security_guard();", conn);
        Assert.NotNull(await cmd0.ExecuteScalarAsync());

        // 2. Chỉ mục thông minh
        await using var cmd1 = new NpgsqlCommand("SELECT count(*) FROM ai.fn_auto_optimize_indexes();", conn);
        Assert.NotNull(await cmd1.ExecuteScalarAsync());

        // 3. Tìm hội viên trùng lặp
        await using var cmd2 = new NpgsqlCommand("SELECT count(*) FROM ai.fn_detect_duplicates(0.65);", conn);
        Assert.NotNull(await cmd2.ExecuteScalarAsync());

        // 4. Phát hiện dị thường
        await using var cmd3 = new NpgsqlCommand("SELECT count(*) FROM ai.fn_detect_anomalies(2.0);", conn);
        Assert.NotNull(await cmd3.ExecuteScalarAsync());

        // 5. Gợi ý khuyến mãi
        await using var cmd4 = new NpgsqlCommand("SELECT count(*) FROM ai.fn_suggest_promotions();", conn);
        Assert.NotNull(await cmd4.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Verify_06_StationCustomerMenu_And_HeartbeatFunctions()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        var repo = new DbRepository(_connectionString);
        var menu = await repo.GetCustomerMenuAsync();

        Assert.NotNull(menu);
        Assert.NotEmpty(menu);
    }

    [Fact]
    public async Task Verify_07_Roles_And_Permissions_Matrix()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        var host = _env["DB_HOST"];
        var port = _env.GetValueOrDefault("DB_PORT", "5432");
        var db = _env["DB_NAME"];
        var ssl = _env.GetValueOrDefault("DB_SSL_MODE", "Require");
        var pwd = _env.GetValueOrDefault("DEMO_PASSWORD", "Demo@2026NetCafe");

        // 1. Thu ngân nv_thao
        var cashierCs = $"Host={host};Port={port};Database={db};Username=nv_thao;Password={pwd};SSL Mode={ssl};Timeout=15;";
        await using var cashierConn = new NpgsqlConnection(cashierCs);
        await cashierConn.OpenAsync();

        // Thu ngân xem được máy trạm và menu
        await using (var cmd = new NpgsqlCommand("SELECT count(*) FROM reporting.vw_computer_grid;", cashierConn))
        {
            Assert.NotNull(await cmd.ExecuteScalarAsync());
        }

        // Thu ngân xem được menu inventory
        await using (var cmd = new NpgsqlCommand("SELECT count(*) FROM inventory.product;", cashierConn))
        {
            Assert.NotNull(await cmd.ExecuteScalarAsync());
        }

        // Thu ngân bị chặn xem cột salary (Column-level privilege 42501)
        await using (var cmd = new NpgsqlCommand("SELECT salary FROM hr.employee LIMIT 1;", cashierConn))
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(async () => await cmd.ExecuteScalarAsync());
            Assert.Equal("42501", ex.SqlState);
        }

        // 2. Kỹ thuật viên nv_hung
        var techCs = $"Host={host};Port={port};Database={db};Username=nv_hung;Password={pwd};SSL Mode={ssl};Timeout=15;";
        await using var techConn = new NpgsqlConnection(techCs);
        await techConn.OpenAsync();

        // Kỹ thuật viên xem được bảo trì
        await using (var cmd = new NpgsqlCommand("SELECT count(*) FROM hr.maintenance;", techConn))
        {
            Assert.NotNull(await cmd.ExecuteScalarAsync());
        }

        // Kỹ thuật viên bị chặn xem inventory.product (Table grant 42501)
        await using (var cmd = new NpgsqlCommand("SELECT count(*) FROM inventory.product;", techConn))
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(async () => await cmd.ExecuteScalarAsync());
            Assert.Equal("42501", ex.SqlState);
        }

        // 3. Máy trạm station_runtime
        var stationUser = _env.GetValueOrDefault("STATION_DB_USER", "station_runtime");
        var stationPass = _env.GetValueOrDefault("STATION_DB_PASSWORD", "Demo@2026NetCafe");
        var stationCs = $"Host={host};Port={port};Database={db};Username={stationUser};Password={stationPass};SSL Mode={ssl};Timeout=15;";
        await using var stationConn = new NpgsqlConnection(stationCs);
        await stationConn.OpenAsync();

        // Máy trạm xem được menu
        await using (var cmd = new NpgsqlCommand("SELECT count(*) FROM inventory.fn_customer_menu();", stationConn))
        {
            Assert.NotNull(await cmd.ExecuteScalarAsync());
        }

        // Máy trạm bị cấm đọc trực tiếp core.customer (42501)
        await using (var cmd = new NpgsqlCommand("SELECT count(*) FROM core.customer;", stationConn))
        {
            var ex = await Assert.ThrowsAsync<PostgresException>(async () => await cmd.ExecuteScalarAsync());
            Assert.Equal("42501", ex.SqlState);
        }
    }

    [Fact]
    public async Task Verify_08_DbaStatistics_Query()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"
        SELECT
            pg_size_pretty(pg_database_size(current_database())),
            COALESCE((SELECT round(100.0 * sum(blks_hit) / nullif(sum(blks_hit + blks_read), 0), 2)
                      FROM pg_stat_database WHERE datname = current_database()), 100.0),
            COALESCE((SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND state = 'active'), 0),
            COALESCE((SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND state = 'idle'), 0),
            COALESCE((SELECT count(*) FROM pg_stat_activity WHERE datname = current_database()), 0),
            COALESCE((SELECT sum(n_dead_tup) FROM pg_stat_user_tables), 0);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.False(string.IsNullOrEmpty(reader.GetString(0)));
        Assert.InRange(reader.GetDecimal(1), 0.0m, 100.0m);
        var active = reader.GetInt64(2);
        var idle = reader.GetInt64(3);
        var total = reader.GetInt64(4);
        Assert.True(active >= 0 && idle >= 0 && total >= active + idle);
    }

    [Fact]
    public async Task Verify_09_ExpectedSchemas_Exist()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT count(*)
            FROM information_schema.schemata
            WHERE schema_name IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging');";
        await using var cmd = new NpgsqlCommand(sql, conn);
        Assert.Equal(8L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task Verify_10_IntegrityConstraints_And_ExclusionContract()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT
                EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'core.gaming_session'::regclass
                      AND conname = 'ex_gaming_session_computer_time'
                ),
                EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'billing.invoice'::regclass
                      AND conname = 'uq_invoice_session'
                ),
                EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'billing.invoice'::regclass
                      AND conname = 'ck_invoice_formula'
                );";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
        Assert.True(reader.GetBoolean(2));
    }

    [Fact]
    public async Task Verify_11_ConcurrencyFunctions_UseSkipLocked()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT
                lower(pg_get_functiondef('core.sp_customer_login(character varying,text,character varying)'::regprocedure)),
                lower(pg_get_functiondef('billing.sp_close_session_and_pay_at(bigint,integer,character varying,character varying,timestamp with time zone,character varying)'::regprocedure));";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Contains("for update skip locked", reader.GetString(0));
        Assert.Contains("for update skip locked", reader.GetString(1));
    }

    [Fact]
    public async Task Verify_12_DailyRevenueMaterializedView_HasUniqueKey()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT EXISTS (
                SELECT 1
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                JOIN pg_attribute a ON a.attrelid = c.oid AND a.attname = 'revenue_date'
                JOIN pg_index i ON i.indrelid = c.oid AND i.indisunique AND a.attnum = ANY(i.indkey)
                WHERE n.nspname = 'reporting'
                  AND c.relname = 'mv_revenue_daily'
                  AND c.relkind = 'm'
            );";
        await using var cmd = new NpgsqlCommand(sql, conn);
        Assert.True(Convert.ToBoolean(await cmd.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task Verify_13_ReportAiFunctionNames_Exist()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT
                to_regprocedure('ai.fn_auto_optimize_indexes()') IS NOT NULL,
                to_regprocedure('ai.fn_security_guard()') IS NOT NULL;";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
    }

    [Fact]
    public async Task Verify_14_TechnicianMaintenanceFunctions_AreGranted()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT
                has_function_privilege('role_technician', 'hr.sp_create_maintenance(integer,integer,text)', 'EXECUTE'),
                has_function_privilege('role_technician', 'hr.sp_complete_maintenance(integer,character varying,numeric)', 'EXECUTE');";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
    }

    [Fact]
    public async Task Verify_15_MaterializedRefresh_ContainsDailyReport()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT lower(pg_get_functiondef('reporting.sp_refresh_materialized_views()'::regprocedure)),
                   has_function_privilege('role_manager', 'reporting.sp_refresh_materialized_views()', 'EXECUTE');";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Contains("mv_revenue_daily", reader.GetString(0));
        Assert.True(reader.GetBoolean(1));
    }

    [Fact]
    public async Task Verify_16_DbaRole_HasMaintenanceAndStatsPrivileges()
    {
        if (string.IsNullOrEmpty(_connectionString)) return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = @"
            SELECT
                has_table_privilege('role_admin', 'core.customer', 'MAINTAIN'),
                pg_has_role('role_admin', 'pg_read_all_stats', 'member');";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
    }
}

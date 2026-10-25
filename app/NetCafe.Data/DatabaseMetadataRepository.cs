// Trách nhiệm: preflight metadata cho Admin; không sửa dữ liệu và không chạy seed/reset.
// Liên kết: MainForm.cs dùng kết quả để bật/tắt reconcile và hiển thị đúng database.

using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task<DatabaseRuntimeInfo> GetDatabaseRuntimeInfoAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            const string sql = @"SELECT current_database(), current_user, COALESCE(inet_server_addr()::text, 'local'),
                                    current_setting('server_version'),
                                    to_regclass('core.service_request') IS NOT NULL,
                                    to_regprocedure('core.sp_reconcile_stale_sessions(integer)') IS NOT NULL,
                                    (SELECT count(*) = 4
                                     FROM information_schema.columns
                                     WHERE table_schema = 'reporting'
                                       AND table_name = 'vw_computer_grid'
                                       AND column_name = ANY (ARRAY[
                                           'current_play_amount',
                                           'current_service_amount',
                                           'current_balance',
                                           'last_heartbeat_at']::text[])),
                                    to_regprocedure('inventory.fn_customer_menu_v2()') IS NOT NULL,
                                    to_regprocedure('core.sp_station_logout(bigint,integer,integer)') IS NOT NULL,
                                    to_regclass('reporting.mv_revenue_daily') IS NOT NULL,
                                    has_table_privilege(current_user, 'core.customer', 'MAINTAIN')
                                    AND has_table_privilege(current_user, 'core.computer', 'MAINTAIN')
                                    AND has_table_privilege(current_user, 'core.gaming_session', 'MAINTAIN');";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Database không trả về metadata preflight.");
        }

        return new DatabaseRuntimeInfo
        {
            Database = reader.GetString(0),
            User = reader.GetString(1),
            Host = reader.GetString(2),
            ServerVersion = reader.GetString(3),
            HasStationMigration = reader.GetBoolean(4),
            HasReconcileFunction = reader.GetBoolean(5),
            HasComputerGridContract = reader.GetBoolean(6),
            HasCustomerMenuContract = reader.GetBoolean(7),
            HasStationLogoutContract = reader.GetBoolean(8),
            HasDailyRevenueMaterializedView = reader.GetBoolean(9),
            CanRunVacuum = reader.GetBoolean(10)
            };
        });
    }

    public async Task<bool> HasFunctionAsync(string qualifiedSignature, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT to_regprocedure(@signature) IS NOT NULL;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("signature", qualifiedSignature);
        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<DbaStatisticsInfo> GetDbaStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            const string sql = @"
            SELECT
                pg_size_pretty(pg_database_size(current_database())),
                COALESCE((SELECT round(100.0 * sum(blks_hit) / nullif(sum(blks_hit + blks_read), 0), 2)
                          FROM pg_stat_database WHERE datname = current_database()), 100.0),
                COALESCE((SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND state = 'active'), 0),
                COALESCE((SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND state = 'idle'), 0),
                COALESCE((SELECT count(*) FROM pg_stat_activity WHERE datname = current_database()), 0),
                COALESCE((SELECT sum(n_dead_tup) FROM pg_stat_user_tables), 0);";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new DbaStatisticsInfo();
            }

            return new DbaStatisticsInfo
            {
                DatabaseSize = reader.GetString(0),
                CacheHitRatio = reader.IsDBNull(1) ? null : reader.GetDecimal(1),
                ActiveConnections = Convert.ToInt32(reader.GetInt64(2)),
                IdleConnections = Convert.ToInt32(reader.GetInt64(3)),
                TotalConnections = Convert.ToInt32(reader.GetInt64(4)),
                TotalDeadTuples = Convert.ToInt64(reader.GetInt64(5))
            };
        });
    }
}

public sealed record DbaStatisticsInfo
{
    public string DatabaseSize { get; init; } = "N/A";
    public decimal? CacheHitRatio { get; init; }
    public int ActiveConnections { get; init; }
    public int IdleConnections { get; init; }
    public int TotalConnections { get; init; }
    public long TotalDeadTuples { get; init; }
}

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
                                    to_regprocedure('core.sp_station_logout(bigint,integer,integer)') IS NOT NULL;";
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
            HasStationLogoutContract = reader.GetBoolean(8)
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
}

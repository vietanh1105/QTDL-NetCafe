// Trách nhiệm: query read-only cho các Admin UserControl; không bypass repository.
// Liên kết: reporting views và các bảng vận hành trong PostgreSQL.

using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task<IReadOnlyList<DailyRevenueItem>> GetDailyRevenueAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT revenue_date, total_invoices, total_play_amount, total_service_amount, total_discount_amount, net_revenue FROM reporting.mv_revenue_daily ORDER BY revenue_date DESC LIMIT 365;";
        return await ReadAsync(sql, reader => new DailyRevenueItem
        {
            RevenueDate = reader.GetFieldValue<DateTime>(0),
            TotalInvoices = reader.GetInt64(1),
            TotalPlayAmount = reader.GetDecimal(2),
            TotalServiceAmount = reader.GetDecimal(3),
            TotalDiscountAmount = reader.GetDecimal(4),
            NetRevenue = reader.GetDecimal(5)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ComputerUsageItem>> GetComputerUsageAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT computer_id, computer_code, computer_name, room_name, total_sessions, total_hours_played, total_play_revenue FROM reporting.vw_computer_usage;";
        return await ReadAsync(sql, reader => new ComputerUsageItem
        {
            ComputerId = reader.GetInt32(0),
            ComputerCode = reader.GetString(1),
            ComputerName = reader.GetString(2),
            RoomName = reader.GetString(3),
            TotalSessions = reader.GetInt64(4),
            TotalHoursPlayed = reader.GetDecimal(5),
            TotalPlayRevenue = reader.GetDecimal(6)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<TopCustomerItem>> GetTopCustomersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT customer_id, full_name, phone, membership_tier, loyalty_points, total_sessions, total_spent, last_played_at FROM reporting.mv_top_customers LIMIT 100;";
        return await ReadAsync(sql, reader => new TopCustomerItem
        {
            CustomerId = reader.GetInt32(0),
            FullName = reader.GetString(1),
            Phone = reader.GetString(2),
            MembershipTier = reader.GetString(3),
            LoyaltyPoints = reader.GetInt32(4),
            TotalSessions = reader.GetInt64(5),
            TotalSpent = reader.GetDecimal(6),
            LastPlayedAt = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTime>(7)
        }, cancellationToken);
    }

    public async Task UpdateServiceRequestStatusAsync(
        long requestId,
        string status,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT core.sp_update_service_request_status(@requestId, @status, @employeeId);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("requestId", requestId);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("employeeId", employeeId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

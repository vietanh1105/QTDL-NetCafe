// Trách nhiệm: query read-only cho các Admin UserControl; không bypass repository.
// Liên kết: reporting views và core.booking trong PostgreSQL.

using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task<IReadOnlyList<DailyRevenueItem>> GetDailyRevenueAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT revenue_date, total_invoices, total_play_amount, total_service_amount, total_discount_amount, net_revenue FROM reporting.vw_daily_revenue LIMIT 365;";
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

    public async Task<IReadOnlyList<BookingItem>> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT b.booking_id, c.full_name, cp.computer_code,
                                    b.booked_from, b.booked_to, b.status
                             FROM core.booking b
                             JOIN core.customer c ON c.customer_id = b.customer_id
                             JOIN core.computer cp ON cp.computer_id = b.computer_id
                             ORDER BY b.booking_id DESC;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<BookingItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new BookingItem
            {
                BookingId = reader.GetInt32(0),
                CustomerName = reader.GetString(1),
                ComputerCode = reader.GetString(2),
                BookedFrom = reader.GetFieldValue<DateTimeOffset>(3),
                BookedTo = reader.GetFieldValue<DateTimeOffset>(4),
                Status = reader.GetString(5)
            });
        }
        return result;
    }

    public async Task UpdateServiceRequestStatusAsync(
        long requestId,
        string status,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE core.service_request
                             SET status = @status,
                                 handled_by = @employeeId,
                                 acknowledged_at = CASE WHEN @status = 'acknowledged' THEN clock_timestamp() ELSE acknowledged_at END,
                                 resolved_at = CASE WHEN @status IN ('resolved', 'cancelled') THEN clock_timestamp() ELSE resolved_at END
                             WHERE service_request_id = @requestId;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("requestId", requestId);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("employeeId", employeeId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

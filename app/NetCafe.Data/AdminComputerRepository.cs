// Trách nhiệm: thao tác trạng thái máy cho Admin qua DbRepository, có kiểm tra trạng thái hợp lệ.
// Liên kết: ComputerActionDialog và bảng core.computer; không để UI mở connection trực tiếp.

using System.Diagnostics;
using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task SetComputerStatusAsync(int computerId, string status, CancellationToken cancellationToken = default)
    {
        var allowed = new[] { "available", "maintenance", "broken" };
        if (!allowed.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Trạng thái máy không hợp lệ: {status}", nameof(status));
        }

        const string sql = "UPDATE core.computer SET status = @status WHERE computer_id = @computer_id;";
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("status", status.ToLowerInvariant());
            command.Parameters.AddWithValue("computer_id", computerId);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                throw new InvalidOperationException($"Không tìm thấy máy có ID {computerId}.");
            }
            LogSql(sql, $"computer_id={computerId};status={status}", stopwatch.ElapsedMilliseconds, true, null);
        }
        catch (Exception exception)
        {
            LogSql(sql, $"computer_id={computerId};status={status}", stopwatch.ElapsedMilliseconds, false, exception.Message);
            throw;
        }
    }
}

// Trách nhiệm: CRUD, reset mật khẩu và kích hoạt khách hàng cho Admin.
// Liên kết: core.customer; password luôn hash bằng pgcrypto crypt() trong PostgreSQL.

using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task<int> CreateCustomerAsync(
        string fullName,
        string phone,
        string? email,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT core.sp_create_customer(@fullName, @phone, @email, @username, @password);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("fullName", fullName.Trim());
        command.Parameters.AddWithValue("phone", phone.Trim());
        command.Parameters.AddWithValue("email", email?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("username", username.Trim());
        command.Parameters.AddWithValue("password", password);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateCustomerAsync(
        int customerId,
        string fullName,
        string phone,
        string? email,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT core.sp_update_customer(@customerId, @fullName, @phone, @email);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("fullName", fullName.Trim());
        command.Parameters.AddWithValue("phone", phone.Trim());
        command.Parameters.AddWithValue("email", email?.Trim() ?? string.Empty);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<decimal> UpdateCustomerAndTopUpAsync(
        int customerId,
        string fullName,
        string phone,
        string? email,
        int employeeId,
        decimal amount,
        string paymentMethod,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string updateSql = "SELECT core.sp_update_customer(@customerId, @fullName, @phone, @email);";
        await using var updateCommand = new NpgsqlCommand(updateSql, connection, transaction);
        updateCommand.Parameters.AddWithValue("customerId", customerId);
        updateCommand.Parameters.AddWithValue("fullName", fullName.Trim());
        updateCommand.Parameters.AddWithValue("phone", phone.Trim());
        updateCommand.Parameters.AddWithValue("email", email?.Trim() ?? string.Empty);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        const string topUpSql = "SELECT billing.sp_top_up(@customerId, @employeeId, @amount, @paymentMethod, @reference);";
        await using var topUpCommand = new NpgsqlCommand(topUpSql, connection, transaction);
        topUpCommand.Parameters.AddWithValue("customerId", customerId);
        topUpCommand.Parameters.AddWithValue("employeeId", employeeId);
        topUpCommand.Parameters.AddWithValue("amount", amount);
        topUpCommand.Parameters.AddWithValue("paymentMethod", paymentMethod);
        topUpCommand.Parameters.AddWithValue("reference", (object?)reference ?? DBNull.Value);
        var newBalance = Convert.ToDecimal(await topUpCommand.ExecuteScalarAsync(cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return newBalance;
    }

    public async Task SetCustomerActiveAsync(int customerId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT core.sp_set_customer_active(@customerId, @isActive);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("isActive", isActive);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ResetCustomerPasswordAsync(int customerId, string password, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT core.sp_reset_customer_password(@customerId, @password);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("password", password);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

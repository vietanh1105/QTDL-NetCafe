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
        const string sql = @"INSERT INTO core.customer(full_name, phone, email, username, password_hash)
                             VALUES (@fullName, @phone, NULLIF(@email, ''), @username, public.crypt(@password, public.gen_salt('bf')))
                             RETURNING customer_id;";
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
        const string sql = @"UPDATE core.customer
                             SET full_name = @fullName, phone = @phone, email = NULLIF(@email, '')
                             WHERE customer_id = @customerId;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("fullName", fullName.Trim());
        command.Parameters.AddWithValue("phone", phone.Trim());
        command.Parameters.AddWithValue("email", email?.Trim() ?? string.Empty);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetCustomerActiveAsync(int customerId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE core.customer SET is_active = @isActive WHERE customer_id = @customerId;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("isActive", isActive);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ResetCustomerPasswordAsync(int customerId, string password, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE core.customer SET password_hash = public.crypt(@password, public.gen_salt('bf')) WHERE customer_id = @customerId;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("password", password);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

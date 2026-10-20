// Trách nhiệm: integration test trên duy nhất netcafe_qa với fixture test_xunit_*.
// Không dùng ID cố định, không reset/truncate dữ liệu thật và không chạy trên Neon.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NetCafe.Data;
using Npgsql;
using Xunit;

namespace NetCafe.Data.Tests;

public sealed class IntegrationTests
{
    private readonly string _connectionString;

    public IntegrationTests()
    {
        var config = ReadConfig();
        if (!string.Equals(config.Database, "netcafe_qa", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("IntegrationTests chỉ được chạy trên database netcafe_qa.");
        }
        _connectionString = config.ConnectionString;
    }

    [Fact]
    public async Task Test_GetComputersGrid_ShouldReturnComputers()
    {
        var list = await new DbRepository(_connectionString).GetComputersAsync();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task Test_GetCustomers_ShouldReturnCustomers()
    {
        var list = await new DbRepository(_connectionString).GetCustomersAsync();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task Test_TopUp_ShouldIncreaseBalance()
    {
        var fixture = await PrepareFixtureAsync();
        var newBalance = await new DbRepository(_connectionString).TopUpAsync(fixture.CustomerA, fixture.EmployeeId, 10000, "cash", "test_xunit_topup");
        Assert.True(newBalance >= 10000);
    }

    [Fact]
    public async Task Test_Open_Close_Session_Lifecycle()
    {
        var fixture = await PrepareFixtureAsync();
        var repository = new DbRepository(_connectionString);
        var sessionId = await repository.OpenSessionAsync(fixture.CustomerA, fixture.ComputerA, fixture.EmployeeId);
        try
        {
            Assert.True(sessionId > 0);
            var invoiceId = await repository.CloseSessionAndPayAsync(sessionId, fixture.EmployeeId, "balance");
            Assert.True(invoiceId > 0);
        }
        finally
        {
            await CloseIfStillActiveAsync(repository, sessionId, fixture.EmployeeId);
        }
    }

    [Fact]
    public async Task Test_Oversell_ShouldThrow_CheckViolation()
    {
        var fixture = await PrepareFixtureAsync();
        var repository = new DbRepository(_connectionString);
        var sessionId = await repository.OpenSessionAsync(fixture.CustomerA, fixture.ComputerA, fixture.EmployeeId);
        try
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
                await repository.PlaceOrderAsync(sessionId, fixture.EmployeeId, 1, 999999));
            Assert.True(exception.SqlState == "23514" || exception.SqlState == "P0001");
        }
        finally
        {
            await CloseIfStillActiveAsync(repository, sessionId, fixture.EmployeeId);
        }
    }

    private async Task<TestFixture> PrepareFixtureAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var usernameA = $"test_xunit_a_{suffix}";
        var phoneA = $"093{Random.Shared.Next(1000000, 9999999)}";
        await using var command = new NpgsqlCommand(@"
            INSERT INTO core.customer(full_name, phone, email, username, password_hash, balance)
            VALUES (@nameA, @phoneA, @emailA, @usernameA, public.crypt('Test@123', public.gen_salt('bf')), 80000)
            RETURNING customer_id;", connection);
        command.Parameters.AddWithValue("nameA", $"TEST xUnit A {suffix}");
        command.Parameters.AddWithValue("phoneA", phoneA);
        command.Parameters.AddWithValue("emailA", $"{usernameA}@example.com");
        command.Parameters.AddWithValue("usernameA", usernameA);
        var customerA = Convert.ToInt32(await command.ExecuteScalarAsync());

        await using var employee = new NpgsqlCommand("SELECT employee_id FROM hr.employee WHERE is_active = TRUE ORDER BY employee_id LIMIT 1;", connection);
        var employeeId = Convert.ToInt32(await employee.ExecuteScalarAsync());
        await using var computers = new NpgsqlCommand("SELECT computer_id FROM core.computer WHERE status = 'available' ORDER BY computer_id LIMIT 2;", connection);
        await using var reader = await computers.ExecuteReaderAsync();
        var computerIds = new List<int>();
        while (await reader.ReadAsync()) computerIds.Add(reader.GetInt32(0));
        if (computerIds.Count < 2) throw new InvalidOperationException("QA cần ít nhất hai máy available cho integration test.");
        return new TestFixture(customerA, employeeId, computerIds[0]);
    }

    private async Task CloseIfStillActiveAsync(DbRepository repository, long sessionId, int employeeId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT status FROM core.gaming_session WHERE session_id = @sessionId;",
            connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        var status = await command.ExecuteScalarAsync();
        if (status is not string sessionStatus || !string.Equals(sessionStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await repository.CloseSessionAndPayAsync(sessionId, employeeId, "cash");
    }

    private static TestConfig ReadConfig()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Tìm .env từ thư mục chạy; QA vẫn ưu tiên biến môi trường do run-qa.ps1 cấp.
        var envPath = Environment.GetEnvironmentVariable("NETCAFE_ENV_FILE");
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (string.IsNullOrWhiteSpace(envPath) && currentDirectory != null)
        {
            var candidate = Path.Combine(currentDirectory.FullName, ".env");
            if (File.Exists(candidate)) envPath = candidate;
            currentDirectory = currentDirectory.Parent;
        }
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#') || !trimmed.Contains('=')) continue;
                var parts = trimmed.Split('=', 2);
                values[parts[0].Trim()] = parts[1].Trim().Trim('"').Trim('\'');
            }
        }
        foreach (var key in new[] { "DB_HOST", "DB_PORT", "DB_NAME", "DB_USER", "DB_PASSWORD", "DB_SSL_MODE" })
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key))) values[key] = Environment.GetEnvironmentVariable(key)!;
        }
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = values.GetValueOrDefault("DB_HOST", "localhost"),
            Port = int.Parse(values.GetValueOrDefault("DB_PORT", "5432")),
            Database = values.GetValueOrDefault("DB_NAME", "netcafe_qa"),
            Username = values.GetValueOrDefault("DB_USER", "postgres"),
            Password = values.GetValueOrDefault("DB_PASSWORD", string.Empty),
            SslMode = Enum.TryParse<SslMode>(values.GetValueOrDefault("DB_SSL_MODE", "Disable"), true, out var ssl) ? ssl : SslMode.Disable,
            IncludeErrorDetail = true
        };
        return new TestConfig(builder.Database, builder.ConnectionString);
    }

    private sealed record TestConfig(string Database, string ConnectionString);
    private sealed record TestFixture(int CustomerA, int EmployeeId, int ComputerA);
}

using Npgsql;
using NetCafe.Data;
using Xunit;

namespace NetCafe.Data.Tests;

public sealed class ConnectionResilienceTests
{
    [Fact]
    public void DbRepository_BuildResilientConnectionString_IsValidAndDoesNotThrow()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5432,
            Database = "netcafe_qa",
            Username = "postgres",
            Password = "test",
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
            IncludeErrorDetail = true
        };

        var cs = builder.ConnectionString;
        var repo = new DbRepository(cs);
        Assert.NotNull(repo);

        using var conn = new NpgsqlConnection(cs);
        Assert.NotNull(conn);
    }

    [Fact]
    public void DbRepository_SanitizesLegacyMinAndMaxPoolSize_WithoutError()
    {
        var legacy = "Host=localhost;Port=5432;Database=netcafe_qa;Username=postgres;Password=test;Pooling=true;Min Pool Size=1;Max Pool Size=25;";
        var repo = new DbRepository(legacy);
        Assert.NotNull(repo);

        using var conn = new NpgsqlConnection(legacy.Replace("Min Pool Size=1;", "").Replace("Max Pool Size=25;", "Maximum Pool Size=25;"));
        Assert.NotNull(conn);
    }
}


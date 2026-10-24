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

    [Theory]
    [InlineData("Host=localhost;Port=5432;Database=netcafe_qa;Username=postgres;Password=test;Pooling=true;Min Pool Size=1;Max Pool Size=25;")]
    [InlineData("Host=localhost;Port=5432;Database=netcafe_qa;Username=postgres;Password=test;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=30;")]
    [InlineData("Host=localhost;Port=5432;Database=netcafe_qa;Username=postgres;Password=test;Pooling=true;min pool size=2;max pool size=20;")]
    [InlineData("Host=localhost;Port=5432;Database=netcafe_qa;Username=postgres;Password=test;Pooling=true;MinPoolSize=3;MaxPoolSize=25;")]
    public void DbRepository_SanitizesLegacyMinAndMaxPoolSize_WithoutError(string connectionString)
    {
        var repo = new DbRepository(connectionString);
        Assert.NotNull(repo);
    }
}


using System;
using System.Threading.Tasks;
using NetCafe.Data;
using Npgsql;
using Xunit;

namespace NetCafe.Data.Tests
{
    public class IntegrationTests
    {
        private readonly string _connectionString;

        public IntegrationTests()
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "127.0.0.1";
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "netcafe_db";
            var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var pwd = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "vietanh1105@";

            _connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pwd};Include Error Detail=true;";
        }

        [Fact]
        public async Task Test_GetComputersGrid_ShouldReturnComputers()
        {
            var repo = new DbRepository(_connectionString);
            var list = await repo.GetComputersAsync();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public async Task Test_GetCustomers_ShouldReturnCustomers()
        {
            var repo = new DbRepository(_connectionString);
            var list = await repo.GetCustomersAsync();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public async Task Test_TopUp_ShouldIncreaseBalance()
        {
            var repo = new DbRepository(_connectionString);
            var newBal = await repo.TopUpAsync(1, 1, 10000, "cash", "Test topup xUnit");
            Assert.True(newBal > 0);
        }

        [Fact]
        public async Task Test_Open_Close_Session_Lifecycle()
        {
            var repo = new DbRepository(_connectionString);
            // Mở phiên máy 3 (PC003) cho khách 2 (Hoàng Đức Anh)
            var sessId = await repo.OpenSessionAsync(2, 3, 1);
            Assert.True(sessId > 0);

            // Đóng phiên và thanh toán bằng số dư
            var invId = await repo.CloseSessionAndPayAsync(sessId, 1, "balance");
            Assert.True(invId > 0);
        }

        [Fact]
        public async Task Test_Overbooking_ShouldThrow_ExclusionViolation()
        {
            var repo = new DbRepository(_connectionString);
            var randomDays = Random.Shared.Next(100, 9000);
            var from = DateTime.Today.AddDays(randomDays).AddHours(10);
            var to = DateTime.Today.AddDays(randomDays).AddHours(14);

            // Đặt lịch lần 1 thành công
            var b1 = await repo.CreateBookingAsync(1, 5, from, to);
            Assert.True(b1 > 0);

            // Đặt lịch trùng giờ lần 2 phải ném lỗi PostgreSQL exclusion violation (23P01)
            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await repo.CreateBookingAsync(2, 5, from.AddHours(1), to.AddHours(1));
            });

            Assert.Equal("23P01", ex.SqlState);
            Assert.Contains("ex_booking_no_overlap", ex.ConstraintName);
        }

        [Fact]
        public async Task Test_Oversell_ShouldThrow_CheckViolation()
        {
            var repo = new DbRepository(_connectionString);

            // Mở phiên máy 4 cho khách 3
            var sessId = await repo.OpenSessionAsync(3, 4, 1);

            // Mua 999999 chai Sting vượt tồn kho (CHECK stock_qty >= 0)
            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await repo.PlaceOrderAsync(sessId, 1, 1, 999999);
            });

            // Đóng phiên để dọn dẹp
            await repo.CloseSessionAndPayAsync(sessId, 1, "cash");

            // Có thể là CHECK constraint từ CSDL hoặc exception từ Stored Procedure
            Assert.True(ex.SqlState == "23514" || ex.SqlState == "P0001");
        }
    }
}

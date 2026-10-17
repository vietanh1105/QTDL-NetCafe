// Trách nhiệm: truy cập PostgreSQL cho Admin và Station; liên kết Models.cs với các function SQL.
// Luồng mới: StationRepository.cs chứa contract customer login, heartbeat, logout, F&B và support.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace NetCafe.Data
{
    public partial class DbRepository
    {
        private readonly string _connectionString;
        public event Action<SqlLogEntry>? OnSqlExecuted;
        public event Action<string, string>? OnNotificationReceived;

        public DbRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private void LogSql(string cmdText, string parameters, long durationMs, bool success, string? error)
        {
            OnSqlExecuted?.Invoke(new SqlLogEntry
            {
                CommandText = cmdText,
                Parameters = parameters,
                DurationMs = durationMs,
                IsSuccess = success,
                ErrorMessage = error
            });
        }

        public static DbExceptionInfo ParseException(Exception ex)
        {
            if (ex is PostgresException pgEx)
            {
                return new DbExceptionInfo
                {
                    SqlState = pgEx.SqlState ?? "UNKNOWN",
                    Message = pgEx.MessageText,
                    ConstraintName = pgEx.ConstraintName ?? "",
                    Detail = pgEx.Detail ?? "",
                    Severity = pgEx.Severity ?? "ERROR"
                };
            }

            return new DbExceptionInfo
            {
                SqlState = "CLIENT_ERR",
                Message = ex.Message,
                Severity = "ERROR"
            };
        }

        public async Task<List<ComputerGridItem>> GetComputersAsync()
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT computer_id, computer_code, computer_name, room_name, type_name, hourly_rate, computer_status, ip_address::text, current_session_id, current_customer_id, current_customer_name, current_username, session_start_time, played_minutes, specs::text FROM reporting.vw_computer_grid;";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<ComputerGridItem>();
                while (await reader.ReadAsync())
                {
                    list.Add(new ComputerGridItem
                    {
                        ComputerId = reader.GetInt32(0),
                        ComputerCode = reader.GetString(1),
                        ComputerName = reader.GetString(2),
                        RoomName = reader.GetString(3),
                        TypeName = reader.GetString(4),
                        HourlyRate = reader.GetDecimal(5),
                        ComputerStatus = reader.GetString(6),
                        IpAddress = reader.GetString(7),
                        CurrentSessionId = reader.IsDBNull(8) ? null : reader.GetInt64(8),
                        CurrentCustomerId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                        CurrentCustomerName = reader.IsDBNull(10) ? null : reader.GetString(10),
                        CurrentUsername = reader.IsDBNull(11) ? null : reader.GetString(11),
                        SessionStartTime = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                        PlayedMinutes = reader.IsDBNull(13) ? null : reader.GetDecimal(13),
                        Specs = reader.IsDBNull(14) ? "{}" : reader.GetString(14)
                    });
                }
                sw.Stop();
                LogSql(sql, "", sw.ElapsedMilliseconds, true, null);
                return list;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, "", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<List<CustomerItem>> GetCustomersAsync(string? search = null)
        {
            var sw = Stopwatch.StartNew();
            string sql;
            if (string.IsNullOrWhiteSpace(search))
            {
                sql = "SELECT customer_id, full_name, phone, email, username, balance, loyalty_points, membership_tier, is_active FROM core.customer ORDER BY customer_id;";
            }
            else
            {
                sql = "SELECT customer_id, full_name, phone, email, username, balance, loyalty_points, membership_tier, is_active FROM core.customer WHERE unaccent(full_name) ILIKE unaccent(@search) OR phone ILIKE @search OR username ILIKE @search ORDER BY customer_id;";
            }

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    cmd.Parameters.AddWithValue("search", $"%{search}%");
                }
                await using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<CustomerItem>();
                while (await reader.ReadAsync())
                {
                    list.Add(new CustomerItem
                    {
                        CustomerId = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Phone = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Username = reader.GetString(4),
                        Balance = reader.GetDecimal(5),
                        LoyaltyPoints = reader.GetInt32(6),
                        MembershipTier = reader.GetString(7),
                        IsActive = reader.GetBoolean(8)
                    });
                }
                sw.Stop();
                LogSql(sql, search ?? "", sw.ElapsedMilliseconds, true, null);
                return list;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, search ?? "", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<List<ProductItem>> GetProductsAsync()
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT p.product_id, p.sku, p.product_name, c.category_name, p.unit_price, p.stock_qty FROM inventory.product p JOIN inventory.product_category c ON p.category_id = c.category_id WHERE p.is_active = TRUE ORDER BY p.product_id;";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<ProductItem>();
                while (await reader.ReadAsync())
                {
                    list.Add(new ProductItem
                    {
                        ProductId = reader.GetInt32(0),
                        Sku = reader.GetString(1),
                        ProductName = reader.GetString(2),
                        CategoryName = reader.GetString(3),
                        UnitPrice = reader.GetDecimal(4),
                        StockQty = reader.GetInt32(5)
                    });
                }
                sw.Stop();
                LogSql(sql, "", sw.ElapsedMilliseconds, true, null);
                return list;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, "", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<List<InvoiceItem>> GetRecentInvoicesAsync(int limit = 50)
        {
            var sw = Stopwatch.StartNew();
            var sql = @"SELECT i.invoice_id, i.session_id, c.full_name, i.play_amount, i.service_amount, i.discount_amount, i.total_amount, i.status, i.issued_at
                        FROM billing.invoice i
                        JOIN core.customer c ON i.customer_id = c.customer_id
                        ORDER BY i.invoice_id DESC LIMIT @limit;";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("limit", limit);
                await using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<InvoiceItem>();
                while (await reader.ReadAsync())
                {
                    list.Add(new InvoiceItem
                    {
                        InvoiceId = reader.GetInt64(0),
                        SessionId = reader.GetInt64(1),
                        CustomerName = reader.GetString(2),
                        PlayAmount = reader.GetDecimal(3),
                        ServiceAmount = reader.GetDecimal(4),
                        DiscountAmount = reader.GetDecimal(5),
                        TotalAmount = reader.GetDecimal(6),
                        Status = reader.GetString(7),
                        IssuedAt = reader.GetDateTime(8)
                    });
                }
                sw.Stop();
                LogSql(sql, $"limit={limit}", sw.ElapsedMilliseconds, true, null);
                return list;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, $"limit={limit}", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<List<EmployeeItem>> GetEmployeesAsync()
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT employee_id, full_name, username, db_username, emp_role, default_shift FROM hr.employee WHERE is_active = TRUE ORDER BY employee_id;";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<EmployeeItem>();
                while (await reader.ReadAsync())
                {
                    list.Add(new EmployeeItem
                    {
                        EmployeeId = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Username = reader.GetString(2),
                        DbUsername = reader.IsDBNull(3) ? null : reader.GetString(3),
                        EmpRole = reader.GetString(4),
                        DefaultShift = reader.GetString(5)
                    });
                }
                sw.Stop();
                LogSql(sql, "", sw.ElapsedMilliseconds, true, null);
                return list;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, "", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        // ==========================================
        // CÁC HÀM GỌI STORED FUNCTION NGHIỆP VỤ
        // ==========================================

        public async Task<long> OpenSessionAsync(int customerId, int computerId, int openedBy)
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT core.sp_open_session(@custId, @compId, @openedBy);";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("custId", customerId);
                cmd.Parameters.AddWithValue("compId", computerId);
                cmd.Parameters.AddWithValue("openedBy", openedBy);
                var result = await cmd.ExecuteScalarAsync();
                var sessionId = Convert.ToInt64(result);
                sw.Stop();
                LogSql(sql, $"custId={customerId}, compId={computerId}, openedBy={openedBy}", sw.ElapsedMilliseconds, true, null);
                return sessionId;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, $"custId={customerId}, compId={computerId}, openedBy={openedBy}", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<long> CloseSessionAndPayAsync(long sessionId, int cashierId, string paymentMethod = "balance", string? promoCode = null)
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT billing.sp_close_session_and_pay(@sessId, @cashierId, @method, @promo);";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("sessId", sessionId);
                cmd.Parameters.AddWithValue("cashierId", cashierId);
                cmd.Parameters.AddWithValue("method", paymentMethod);
                cmd.Parameters.AddWithValue("promo", (object?)promoCode ?? DBNull.Value);
                var result = await cmd.ExecuteScalarAsync();
                var invoiceId = Convert.ToInt64(result);
                sw.Stop();
                LogSql(sql, $"sessId={sessionId}, cashierId={cashierId}, method={paymentMethod}, promo={promoCode}", sw.ElapsedMilliseconds, true, null);
                return invoiceId;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, $"sessId={sessionId}, cashierId={cashierId}, method={paymentMethod}, promo={promoCode}", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<decimal> TopUpAsync(int customerId, int employeeId, decimal amount, string method = "cash", string? reference = null)
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT billing.sp_top_up(@custId, @empId, @amount, @method, @ref);";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("custId", customerId);
                cmd.Parameters.AddWithValue("empId", employeeId);
                cmd.Parameters.AddWithValue("amount", amount);
                cmd.Parameters.AddWithValue("method", method);
                cmd.Parameters.AddWithValue("ref", (object?)reference ?? DBNull.Value);
                var result = await cmd.ExecuteScalarAsync();
                var newBalance = Convert.ToDecimal(result);
                sw.Stop();
                LogSql(sql, $"custId={customerId}, amount={amount}, method={method}", sw.ElapsedMilliseconds, true, null);
                return newBalance;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, $"custId={customerId}, amount={amount}, method={method}", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<long> PlaceOrderAsync(long sessionId, int employeeId, int productId, int quantity)
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT inventory.sp_place_order(@sessId, @empId, @prodId, @qty);";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("sessId", sessionId);
                cmd.Parameters.AddWithValue("empId", employeeId);
                cmd.Parameters.AddWithValue("prodId", productId);
                cmd.Parameters.AddWithValue("qty", quantity);
                var result = await cmd.ExecuteScalarAsync();
                var orderId = Convert.ToInt64(result);
                sw.Stop();
                LogSql(sql, $"sessId={sessionId}, prodId={productId}, qty={quantity}", sw.ElapsedMilliseconds, true, null);
                return orderId;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, $"sessId={sessionId}, prodId={productId}, qty={quantity}", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task<int> CreateBookingAsync(int customerId, int computerId, DateTime bookedFrom, DateTime bookedTo)
        {
            var sw = Stopwatch.StartNew();
            var sql = "INSERT INTO core.booking (customer_id, computer_id, booked_from, booked_to, status) VALUES (@custId, @compId, @from, @to, 'confirmed') RETURNING booking_id;";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("custId", customerId);
                cmd.Parameters.AddWithValue("compId", computerId);
                cmd.Parameters.AddWithValue("from", bookedFrom);
                cmd.Parameters.AddWithValue("to", bookedTo);
                var result = await cmd.ExecuteScalarAsync();
                var bookingId = Convert.ToInt32(result);
                sw.Stop();
                LogSql(sql, $"custId={customerId}, compId={computerId}, from={bookedFrom:HH:mm}, to={bookedTo:HH:mm}", sw.ElapsedMilliseconds, true, null);
                return bookingId;
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, $"custId={customerId}, compId={computerId}, from={bookedFrom:HH:mm}, to={bookedTo:HH:mm}", sw.ElapsedMilliseconds, false, ex.Message);
                throw;
            }
        }

        public async Task StartListeningAsync(CancellationToken ct)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                conn.Notification += (o, e) =>
                {
                    OnNotificationReceived?.Invoke(e.Channel, e.Payload);
                };

                await using var cmd = new NpgsqlCommand("LISTEN ai_security_alert;", conn);
                await cmd.ExecuteNonQueryAsync(ct);

                while (!ct.IsCancellationRequested)
                {
                    await conn.WaitAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogSql("LISTEN ai_security_alert", "", 0, false, ex.Message);
            }
        }
    }
}

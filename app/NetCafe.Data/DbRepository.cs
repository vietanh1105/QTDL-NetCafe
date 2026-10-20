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

        public async Task<List<ComputerGridItem>> GetComputersAsync(CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT computer_id, computer_code, computer_name, room_name, type_name, hourly_rate, computer_status, ip_address::text, current_session_id, current_customer_id, current_customer_name, current_username, session_start_time, played_minutes, current_play_amount, current_service_amount, current_balance, last_heartbeat_at, specs::text FROM reporting.vw_computer_grid;";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var list = new List<ComputerGridItem>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    list.Add(new ComputerGridItem
                    {
                        ComputerId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        ComputerCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        ComputerName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        RoomName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        TypeName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        HourlyRate = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                        ComputerStatus = reader.IsDBNull(6) ? "unknown" : reader.GetString(6),
                        IpAddress = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        CurrentSessionId = reader.IsDBNull(8) ? null : reader.GetInt64(8),
                        CurrentCustomerId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                        CurrentCustomerName = reader.IsDBNull(10) ? null : reader.GetString(10),
                        CurrentUsername = reader.IsDBNull(11) ? null : reader.GetString(11),
                        SessionStartTime = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                        PlayedMinutes = reader.IsDBNull(13) ? null : reader.GetDecimal(13),
                        CurrentPlayAmount = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                        CurrentServiceAmount = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                        CurrentBalance = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                        LastHeartbeatAt = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
                        Specs = reader.IsDBNull(18) ? "{}" : reader.GetString(18)
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
            var sql = "SELECT employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active FROM hr.employee WHERE is_active = TRUE ORDER BY employee_id;";
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
                        Phone = reader.GetString(2),
                        Username = reader.GetString(3),
                        DbUsername = reader.IsDBNull(4) ? null : reader.GetString(4),
                        EmpRole = reader.GetString(5),
                        DefaultShift = reader.GetString(6),
                        HiredDate = reader.GetDateTime(7),
                        IsActive = reader.GetBoolean(8)
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

        public async Task<List<KitchenOrderItem>> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var sql = @"
                SELECT 
                    fo.order_id,
                    fo.session_id,
                    COALESCE(c.computer_code, 'N/A') AS computer_code,
                    COALESCE(cust.full_name, 'Khách vãng lai') AS customer_name,
                    COALESCE(string_agg(p.product_name || ' (x' || fod.quantity || ')', ', '), 'Món ăn') AS items_summary,
                    COALESCE(SUM(fod.quantity * fod.unit_price), 0) AS total_amount,
                    fo.status,
                    fo.ordered_at
                FROM inventory.food_order fo
                LEFT JOIN core.gaming_session s ON s.session_id = fo.session_id
                LEFT JOIN core.computer c ON c.computer_id = s.computer_id
                LEFT JOIN core.customer cust ON cust.customer_id = s.customer_id
                LEFT JOIN inventory.food_order_detail fod ON fod.order_id = fo.order_id
                LEFT JOIN inventory.product p ON p.product_id = fod.product_id
                GROUP BY fo.order_id, fo.session_id, c.computer_code, cust.full_name, fo.status, fo.ordered_at
                ORDER BY 
                    CASE WHEN fo.status = 'placed' THEN 0 ELSE 1 END,
                    fo.ordered_at DESC
                LIMIT 100;";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var list = new List<KitchenOrderItem>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    list.Add(new KitchenOrderItem
                    {
                        OrderId = reader.GetInt64(0),
                        SessionId = reader.GetInt64(1),
                        ComputerCode = reader.IsDBNull(2) ? "N/A" : reader.GetString(2),
                        CustomerName = reader.IsDBNull(3) ? "Khách vãng lai" : reader.GetString(3),
                        ItemsSummary = reader.IsDBNull(4) ? "Món ăn" : reader.GetString(4),
                        TotalAmount = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                        Status = reader.IsDBNull(6) ? "placed" : reader.GetString(6),
                        OrderedAt = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7)
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

        public async Task UpdateFoodOrderStatusAsync(long orderId, string status, int employeeId = 0)
        {
            var sw = Stopwatch.StartNew();
            var sql = "SELECT inventory.sp_update_order_status(@orderId, @status, @empId);";
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("empId", employeeId);
                cmd.Parameters.AddWithValue("orderId", orderId);
                await cmd.ExecuteNonQueryAsync();
                sw.Stop();
                LogSql(sql, $"orderId={orderId}, status={status}", sw.ElapsedMilliseconds, true, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogSql(sql, $"orderId={orderId}, status={status}", sw.ElapsedMilliseconds, false, ex.Message);
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

                await using var cmd = new NpgsqlCommand(
                    "LISTEN ai_security_alert; LISTEN computer_state_changed; LISTEN support_queue_changed; LISTEN food_order_changed;",
                    conn);
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

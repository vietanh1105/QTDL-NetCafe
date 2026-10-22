// Trách nhiệm: repository cho thao tác Admin nhạy cảm và dữ liệu master-detail.
// Liên kết: sql/10_admin_operations.sql, các view Admin F&B, hóa đơn, báo cáo, nhân sự và bảo trì.

using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task<int> ReceiveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT inventory.sp_receive_stock(@productId, @quantity);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("productId", productId);
        command.Parameters.AddWithValue("quantity", quantity);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task VoidInvoiceAsync(long invoiceId, int employeeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT billing.sp_void_invoice(@invoiceId, @employeeId);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("invoiceId", invoiceId);
        command.Parameters.AddWithValue("employeeId", employeeId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RefreshMaterializedViewsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT reporting.sp_refresh_materialized_views();";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RunVacuumAnalyzeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "VACUUM (ANALYZE) core.customer, core.computer, core.gaming_session, inventory.product, inventory.food_order, billing.invoice, hr.employee;",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceItem>> GetInvoicesAsync(
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT i.invoice_id,
                   i.session_id,
                   c.full_name,
                   COALESCE(cp.computer_code, 'N/A'),
                   i.play_amount,
                   i.service_amount,
                   i.discount_amount,
                   i.total_amount,
                   i.status,
                   i.issued_at,
                   COALESCE(pay.method, '')
            FROM billing.invoice AS i
            JOIN core.customer AS c ON c.customer_id = i.customer_id
            LEFT JOIN core.gaming_session AS s ON s.session_id = i.session_id
            LEFT JOIN core.computer AS cp ON cp.computer_id = s.computer_id
            LEFT JOIN LATERAL (
                SELECT p.method
                FROM billing.payment AS p
                WHERE p.invoice_id = i.invoice_id
                ORDER BY p.payment_id DESC
                LIMIT 1
            ) AS pay ON TRUE
            WHERE (@status::varchar IS NULL OR i.status = @status)
              AND (@fromDate::timestamptz IS NULL OR i.issued_at >= @fromDate)
              AND (@toDate::timestamptz IS NULL OR i.issued_at < @toDate)
              AND (@search::varchar IS NULL OR c.full_name ILIKE @search OR i.invoice_id::text ILIKE @search OR cp.computer_code ILIKE @search)
            ORDER BY i.invoice_id DESC;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("fromDate", (object?)ToUtc(from) ?? DBNull.Value);
        command.Parameters.AddWithValue("toDate", (object?)ToUtc(to) ?? DBNull.Value);
        command.Parameters.AddWithValue("search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<InvoiceItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new InvoiceItem
            {
                InvoiceId = reader.GetInt64(0),
                SessionId = reader.GetInt64(1),
                CustomerName = reader.GetString(2),
                ComputerCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                PlayAmount = reader.GetDecimal(4),
                ServiceAmount = reader.GetDecimal(5),
                DiscountAmount = reader.GetDecimal(6),
                TotalAmount = reader.GetDecimal(7),
                Status = reader.GetString(8),
                IssuedAt = reader.GetDateTime(9),
                PaymentMethod = reader.IsDBNull(10) ? string.Empty : reader.GetString(10)
            });
        }
        return rows;
    }

    public async Task<InvoiceDetailItem?> GetInvoiceDetailAsync(long invoiceId, CancellationToken cancellationToken = default)
    {
        const string invoiceSql = @"
            SELECT i.invoice_id, i.session_id, c.full_name, c.username,
                   COALESCE(cp.computer_code, 'N/A'), s.start_time, s.end_time,
                   i.play_amount, i.service_amount, i.discount_amount, i.total_amount,
                   i.status, i.issued_at, COALESCE(pay.method, '')
            FROM billing.invoice AS i
            JOIN core.customer AS c ON c.customer_id = i.customer_id
            LEFT JOIN core.gaming_session AS s ON s.session_id = i.session_id
            LEFT JOIN core.computer AS cp ON cp.computer_id = s.computer_id
            LEFT JOIN LATERAL (
                SELECT p.method FROM billing.payment AS p
                WHERE p.invoice_id = i.invoice_id
                ORDER BY p.payment_id DESC LIMIT 1
            ) AS pay ON TRUE
            WHERE i.invoice_id = @invoiceId;";
        const string lineSql = @"
            SELECT p.product_name, d.quantity, d.unit_price
            FROM inventory.food_order AS o
            JOIN inventory.food_order_detail AS d ON d.order_id = o.order_id
            JOIN inventory.product AS p ON p.product_id = d.product_id
            JOIN core.gaming_session AS s ON s.session_id = o.session_id
            JOIN billing.invoice AS i ON i.session_id = s.session_id
            WHERE i.invoice_id = @invoiceId
              AND o.status <> 'cancelled'
            ORDER BY o.ordered_at, p.product_name;";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var invoiceCommand = new NpgsqlCommand(invoiceSql, connection);
        invoiceCommand.Parameters.AddWithValue("invoiceId", invoiceId);
        await using var reader = await invoiceCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }
        var result = new InvoiceDetailItem
        {
            InvoiceId = reader.GetInt64(0),
            SessionId = reader.GetInt64(1),
            CustomerName = reader.GetString(2),
            Username = reader.GetString(3),
            ComputerCode = reader.GetString(4),
            SessionStart = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            SessionEnd = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            PlayAmount = reader.GetDecimal(7),
            ServiceAmount = reader.GetDecimal(8),
            DiscountAmount = reader.GetDecimal(9),
            TotalAmount = reader.GetDecimal(10),
            Status = reader.GetString(11),
            IssuedAt = reader.GetDateTime(12),
            PaymentMethod = reader.IsDBNull(13) ? string.Empty : reader.GetString(13)
        };
        await reader.CloseAsync();

        await using var lineCommand = new NpgsqlCommand(lineSql, connection);
        lineCommand.Parameters.AddWithValue("invoiceId", invoiceId);
        await using var lineReader = await lineCommand.ExecuteReaderAsync(cancellationToken);
        var lines = new List<InvoiceLineItem>();
        while (await lineReader.ReadAsync(cancellationToken))
        {
            lines.Add(new InvoiceLineItem
            {
                ProductName = lineReader.GetString(0),
                Quantity = lineReader.GetInt32(1),
                UnitPrice = lineReader.GetDecimal(2)
            });
        }
        return result with { Lines = lines };
    }

    public async Task<IReadOnlyList<EmployeeItem>> GetEmployeesWithSalaryAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT employee_id, full_name, phone, username, db_username, emp_role,
                                    default_shift, salary, hired_date, is_active
                             FROM hr.employee ORDER BY employee_id;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<EmployeeItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new EmployeeItem
            {
                EmployeeId = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Phone = reader.GetString(2),
                Username = reader.GetString(3),
                DbUsername = reader.IsDBNull(4) ? null : reader.GetString(4),
                EmpRole = reader.GetString(5),
                DefaultShift = reader.GetString(6),
                Salary = reader.GetDecimal(7),
                HiredDate = reader.GetDateTime(8),
                IsActive = reader.GetBoolean(9)
            });
        }
        return rows;
    }

    public async Task<int> CreateEmployeeAsync(
        string fullName,
        string phone,
        string username,
        string? dbUsername,
        string password,
        string role,
        string shift,
        decimal salary,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT hr.sp_create_employee(@fullName, @phone, @username, @dbUsername, @password, @role, @shift, @salary);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("fullName", fullName);
        command.Parameters.AddWithValue("phone", phone);
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("dbUsername", (object?)dbUsername ?? DBNull.Value);
        command.Parameters.AddWithValue("password", password);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("shift", shift);
        command.Parameters.AddWithValue("salary", salary);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateEmployeeAsync(EmployeeItem employee, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT hr.sp_update_employee(@employeeId, @fullName, @phone, @dbUsername, @role, @shift, @salary);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeId", employee.EmployeeId);
        command.Parameters.AddWithValue("fullName", employee.FullName);
        command.Parameters.AddWithValue("phone", employee.Phone);
        command.Parameters.AddWithValue("dbUsername", (object?)employee.DbUsername ?? DBNull.Value);
        command.Parameters.AddWithValue("role", employee.EmpRole);
        command.Parameters.AddWithValue("shift", employee.DefaultShift);
        command.Parameters.AddWithValue("salary", employee.Salary ?? 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetEmployeeActiveAsync(int employeeId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT hr.sp_set_employee_active(@employeeId, @isActive);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeId", employeeId);
        command.Parameters.AddWithValue("isActive", isActive);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ResetEmployeePasswordAsync(int employeeId, string password, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT hr.sp_reset_employee_password(@employeeId, @password);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("employeeId", employeeId);
        command.Parameters.AddWithValue("password", password);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceItem>> GetMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT m.maintenance_id, m.computer_id, c.computer_code, m.technician_id,
                                    e.full_name, m.issue_description, m.started_at, m.finished_at,
                                    m.cost, m.status
                             FROM hr.maintenance AS m
                             JOIN core.computer AS c ON c.computer_id = m.computer_id
                             JOIN hr.employee AS e ON e.employee_id = m.technician_id
                             ORDER BY m.finished_at NULLS FIRST, m.started_at DESC;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<MaintenanceItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new MaintenanceItem
            {
                MaintenanceId = reader.GetInt32(0),
                ComputerId = reader.GetInt32(1),
                ComputerCode = reader.GetString(2),
                TechnicianId = reader.GetInt32(3),
                TechnicianName = reader.GetString(4),
                IssueDescription = reader.GetString(5),
                StartedAt = reader.GetDateTime(6),
                FinishedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                Cost = reader.GetDecimal(8),
                Status = reader.GetString(9)
            });
        }
        return rows;
    }

    public async Task<int> CreateMaintenanceAsync(int computerId, int technicianId, string issue, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT hr.sp_create_maintenance(@computerId, @technicianId, @issue);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("computerId", computerId);
        command.Parameters.AddWithValue("technicianId", technicianId);
        command.Parameters.AddWithValue("issue", issue);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task CompleteMaintenanceAsync(int maintenanceId, string status, decimal cost, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT hr.sp_complete_maintenance(@maintenanceId, @status, @cost);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("maintenanceId", maintenanceId);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("cost", cost);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }
}

// Trách nhiệm: các function data access cho NetCafe.Client và Admin support/AI.
// Liên kết: StationModels.cs và sql/08_station_customer.sql; không cho UI tự viết SQL.

using System.Diagnostics;
using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task<StationLoginResult> CustomerLoginAsync(
        string username,
        string password,
        string computerCode,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM core.sp_customer_login(@username, @password, @computerCode);";
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("username", username);
            command.Parameters.AddWithValue("password", password);
            command.Parameters.AddWithValue("computerCode", computerCode);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Database không trả về session station.");
            }

            stopwatch.Stop();
            LogSql(sql, $"username={username}, computerCode={computerCode}", stopwatch.ElapsedMilliseconds, true, null);
            return new StationLoginResult
            {
                SessionId = reader.GetInt64(0),
                CustomerId = reader.GetInt32(1),
                ComputerId = reader.GetInt32(2),
                ComputerCode = reader.GetString(3),
                CustomerName = reader.GetString(4),
                Balance = reader.GetDecimal(5),
                HourlyRate = reader.GetDecimal(6),
                StartTime = reader.GetFieldValue<DateTimeOffset>(7)
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogSql(sql, $"username={username}, computerCode={computerCode}", stopwatch.ElapsedMilliseconds, false, exception.Message);
            throw;
        }
    }

    public async Task<StationHeartbeatResult> StationHeartbeatAsync(
        long sessionId,
        int customerId,
        int computerId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM core.sp_station_heartbeat(@sessionId, @customerId, @computerId);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("computerId", computerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Database không trả về heartbeat.");
        }

        return new StationHeartbeatResult
        {
            SessionId = reader.GetInt64(0),
            BalanceRemaining = reader.GetDecimal(1),
            ProjectedPlayAmount = reader.GetDecimal(2),
            ProjectedServiceAmount = reader.GetDecimal(3),
            ProjectedTotalAmount = reader.GetDecimal(4),
            LastHeartbeatAt = reader.GetFieldValue<DateTimeOffset>(5),
            SessionStatus = reader.GetString(6)
        };
    }

    public async Task<long> StationLogoutAsync(
        long sessionId,
        int customerId,
        int computerId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT core.sp_station_logout(@sessionId, @customerId, @computerId);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("computerId", computerId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<long> CustomerPlaceOrderAsync(
        long sessionId,
        int customerId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT inventory.sp_customer_place_order(@sessionId, @customerId, @productId, @quantity);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("productId", productId);
        command.Parameters.AddWithValue("quantity", quantity);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<CustomerOrderStatus>> GetCustomerOrderStatusAsync(
        long sessionId,
        int customerId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM inventory.fn_customer_order_status(@sessionId, @customerId);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("customerId", customerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<CustomerOrderStatus>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CustomerOrderStatus
            {
                OrderId = reader.GetInt64(0),
                Status = reader.GetString(1),
                OrderedAt = reader.GetFieldValue<DateTimeOffset>(2),
                TotalAmount = reader.GetDecimal(3)
            });
        }
        return result;
    }

    public async Task<IReadOnlyList<CustomerMenuItem>> GetCustomerMenuAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM inventory.fn_customer_menu_v2();";
        return await ReadAsync(sql, reader => new CustomerMenuItem
        {
            ProductId = reader.GetInt32(0),
            Sku = reader.GetString(1),
            ProductName = reader.GetString(2),
            UnitPrice = reader.GetDecimal(3),
            StockQty = reader.GetInt32(4)
        }, cancellationToken);
    }

    public async Task<long> CreateServiceRequestAsync(
        long sessionId,
        int customerId,
        int computerId,
        string requestType,
        string message,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT core.sp_create_service_request(@sessionId, @customerId, @computerId, @requestType, @message);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("computerId", computerId);
        command.Parameters.AddWithValue("requestType", requestType);
        command.Parameters.AddWithValue("message", message);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<ServiceRequestItem>> GetServiceRequestsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT r.service_request_id, c.computer_code, COALESCE(u.full_name, ''),
                                    r.request_type, r.message, r.status, r.created_at
                             FROM core.service_request r
                             JOIN core.computer c ON c.computer_id = r.computer_id
                             LEFT JOIN core.customer u ON u.customer_id = r.customer_id
                             ORDER BY r.created_at DESC;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<ServiceRequestItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ServiceRequestItem
            {
                ServiceRequestId = reader.GetInt64(0),
                ComputerCode = reader.GetString(1),
                CustomerName = reader.GetString(2),
                RequestType = reader.GetString(3),
                Message = reader.GetString(4),
                Status = reader.GetString(5),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(6)
            });
        }
        return result;
    }

    public async Task<int> ReconcileStaleSessionsAsync(int timeoutSeconds = 90, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT count(*) FROM core.sp_reconcile_stale_sessions(@timeoutSeconds);";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("timeoutSeconds", timeoutSeconds);
        var count = 0;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            count = Convert.ToInt32(reader.GetInt64(0));
        }
        return count;
    }

    public async Task<IReadOnlyList<AiAlertItem>> GetAiAlertsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT alert_id, risk_score, risk_level, reason, status, created_at FROM ai.ai_alert ORDER BY created_at DESC LIMIT 100;";
        return await ReadAsync(sql, reader => new AiAlertItem
        {
            AlertId = reader.GetInt32(0),
            RiskScore = reader.GetInt32(1),
            RiskLevel = reader.GetString(2),
            Reason = reader.GetString(3),
            Status = reader.GetString(4),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(5)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<IndexRecommendationItem>> RecommendIndexesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT recommendation_id, table_name, column_list, reason, suggested_sql FROM ai.fn_recommend_indexes();";
        return await ReadAsync(sql, reader => new IndexRecommendationItem
        {
            RecommendationId = reader.GetInt32(0),
            TableName = reader.GetString(1),
            ColumnList = reader.GetString(2),
            Reason = reader.GetString(3),
            SuggestedSql = reader.GetString(4)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateCandidateItem>> DetectDuplicatesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT candidate_id, customer_id_a, customer_name_a, customer_id_b, customer_name_b, similarity, reason FROM ai.fn_detect_duplicates(0.65);";
        return await ReadAsync(sql, reader => new DuplicateCandidateItem
        {
            CandidateId = reader.GetInt32(0),
            CustomerIdA = reader.GetInt32(1),
            CustomerNameA = reader.GetString(2),
            CustomerIdB = reader.GetInt32(3),
            CustomerNameB = reader.GetString(4),
            Similarity = reader.GetDecimal(5),
            Reason = reader.GetString(6)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<AnomalyFlagItem>> DetectAnomaliesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT flag_id, anomaly_type, subject_table, subject_pk, metric_value, z_score, status FROM ai.fn_detect_anomalies(2.0);";
        return await ReadAsync(sql, reader => new AnomalyFlagItem
        {
            FlagId = reader.GetInt32(0),
            AnomalyType = reader.GetString(1),
            SubjectTable = reader.GetString(2),
            SubjectPk = reader.GetString(3),
            MetricValue = reader.GetDecimal(4),
            ZScore = reader.GetDecimal(5),
            Status = reader.GetString(6)
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<PromotionSuggestionItem>> SuggestPromotionsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT suggestion_id, suggestion_type, target, details::text FROM ai.fn_suggest_promotions();";
        return await ReadAsync(sql, reader => new PromotionSuggestionItem
        {
            SuggestionId = reader.GetInt32(0),
            SuggestionType = reader.GetString(1),
            Target = reader.GetString(2),
            Details = reader.GetString(3)
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> ReadAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(map(reader));
            }
            return result;
        });
    }
}

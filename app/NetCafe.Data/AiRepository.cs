// Trách nhiệm: đọc snapshot 5 module AI và chạy module chỉ khi Admin bấm nút.
// Liên kết: sql/07_ai_modules.sql; refresh UI không tự refresh materialized view.

using Npgsql;

namespace NetCafe.Data;

public partial class DbRepository
{
    public async Task RunAiModuleAsync(string module, CancellationToken cancellationToken = default)
    {
        var sql = module switch
        {
            "index" => "SELECT ai.fn_auto_optimize_indexes();",
            "duplicate" => "SELECT ai.fn_detect_duplicates(0.65);",
            "anomaly" => "SELECT ai.fn_detect_anomalies(2.0);",
            "promotion" => "SELECT ai.fn_suggest_promotions();",
            "security" => "SELECT count(*) FROM ai.fn_security_guard();",
            _ => throw new ArgumentException($"Module AI không hợp lệ: {module}", nameof(module))
        };

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAiAlertStatusAsync(int alertId, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("new" or "reviewed"))
        {
            throw new ArgumentException("Trạng thái cảnh báo AI không hợp lệ.", nameof(status));
        }

        const string sql = "UPDATE ai.ai_alert SET status = @status WHERE alert_id = @alertId;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("alertId", alertId);
        command.Parameters.AddWithValue("status", status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateDuplicateCandidateStatusAsync(int candidateId, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("pending" or "merged" or "ignored"))
        {
            throw new ArgumentException("Trạng thái duplicate không hợp lệ.", nameof(status));
        }

        const string sql = "UPDATE ai.duplicate_candidate SET status = @status WHERE cand_id = @candidateId;";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("candidateId", candidateId);
        command.Parameters.AddWithValue("status", status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task<IReadOnlyList<IndexRecommendationItem>> GetIndexRecommendationsAsync(CancellationToken ct = default) =>
        ReadAsync("SELECT rec_id, table_name, column_list, reason, suggested_sql FROM ai.index_recommendation ORDER BY rec_id DESC LIMIT 100;", reader => new IndexRecommendationItem
        {
            RecommendationId = reader.GetInt32(0), TableName = reader.GetString(1), ColumnList = reader.GetString(2), Reason = reader.GetString(3), SuggestedSql = reader.GetString(4)
        }, ct);

    public Task<IReadOnlyList<DuplicateCandidateItem>> GetDuplicateCandidatesAsync(CancellationToken ct = default) =>
        ReadAsync(@"SELECT d.cand_id, d.customer_id_a, a.full_name, d.customer_id_b, b.full_name, d.similarity, d.reason, d.status
                    FROM ai.duplicate_candidate d
                    JOIN core.customer a ON a.customer_id = d.customer_id_a
                    JOIN core.customer b ON b.customer_id = d.customer_id_b
                    ORDER BY d.cand_id DESC LIMIT 100;", reader => new DuplicateCandidateItem
        {
            CandidateId = reader.GetInt32(0), CustomerIdA = reader.GetInt32(1), CustomerNameA = reader.GetString(2), CustomerIdB = reader.GetInt32(3), CustomerNameB = reader.GetString(4), Similarity = reader.GetDecimal(5), Reason = reader.GetString(6), Status = reader.GetString(7)
        }, ct);

    public Task<IReadOnlyList<AnomalyFlagItem>> GetAnomalyFlagsAsync(CancellationToken ct = default) =>
        ReadAsync(@"SELECT flag_id, anomaly_type, subject_table, subject_pk, metric_value,
                           COALESCE((threshold_info->>'z_score')::numeric, 0), status
                    FROM ai.anomaly_flag ORDER BY flag_id DESC LIMIT 100;", reader => new AnomalyFlagItem
        {
            FlagId = reader.GetInt32(0), AnomalyType = reader.GetString(1), SubjectTable = reader.GetString(2), SubjectPk = reader.GetString(3), MetricValue = reader.GetDecimal(4), ZScore = reader.GetDecimal(5), Status = reader.GetString(6)
        }, ct);

    public Task<IReadOnlyList<PromotionSuggestionItem>> GetPromotionSuggestionsAsync(CancellationToken ct = default) =>
        ReadAsync("SELECT sug_id, suggestion_type, target, details::text FROM ai.promotion_suggestion ORDER BY sug_id DESC LIMIT 100;", reader => new PromotionSuggestionItem
        {
            SuggestionId = reader.GetInt32(0), SuggestionType = reader.GetString(1), Target = reader.GetString(2), Details = reader.GetString(3)
        }, ct);

    public async Task RunAiModulesAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT count(*) FROM ai.fn_security_guard(); SELECT count(*) FROM ai.fn_auto_optimize_indexes(); SELECT count(*) FROM ai.fn_detect_duplicates(0.65); SELECT count(*) FROM ai.fn_detect_anomalies(2.0); SELECT count(*) FROM ai.fn_suggest_promotions();";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.NextResultAsync(ct)) { }
    }
}

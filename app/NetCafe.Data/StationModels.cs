// Trách nhiệm: DTO cho NetCafe.Client; dữ liệu đến từ function trong sql/08_station_customer.sql.
// Không chứa mật khẩu hoặc logic tính tiền; database là nguồn sự thật cho session và wallet.

namespace NetCafe.Data;

public sealed class StationLoginResult
{
    public long SessionId { get; init; }
    public int CustomerId { get; init; }
    public int ComputerId { get; init; }
    public string ComputerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal HourlyRate { get; init; }
    public DateTimeOffset StartTime { get; init; }
}

public sealed class StationHeartbeatResult
{
    public long SessionId { get; init; }
    public decimal BalanceRemaining { get; init; }
    public decimal ProjectedPlayAmount { get; init; }
    public decimal ProjectedServiceAmount { get; init; }
    public decimal ProjectedTotalAmount { get; init; }
    public DateTimeOffset LastHeartbeatAt { get; init; }
    public string SessionStatus { get; init; } = "active";
}

public sealed class CustomerOrderStatus
{
    public long OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset OrderedAt { get; init; }
    public decimal TotalAmount { get; init; }
}

public sealed class CustomerMenuItem
{
    public int ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int StockQty { get; init; }
}

public sealed class ServiceRequestItem
{
    public long ServiceRequestId { get; init; }
    public string ComputerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string RequestType { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AiAlertItem
{
    public int AlertId { get; init; }
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class IndexRecommendationItem
{
    public int RecommendationId { get; init; }
    public string TableName { get; init; } = string.Empty;
    public string ColumnList { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string SuggestedSql { get; init; } = string.Empty;
}

public sealed class DuplicateCandidateItem
{
    public int CandidateId { get; init; }
    public int CustomerIdA { get; init; }
    public string CustomerNameA { get; init; } = string.Empty;
    public int CustomerIdB { get; init; }
    public string CustomerNameB { get; init; } = string.Empty;
    public decimal Similarity { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class AnomalyFlagItem
{
    public int FlagId { get; init; }
    public string AnomalyType { get; init; } = string.Empty;
    public string SubjectTable { get; init; } = string.Empty;
    public string SubjectPk { get; init; } = string.Empty;
    public decimal MetricValue { get; init; }
    public decimal ZScore { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class PromotionSuggestionItem
{
    public int SuggestionId { get; init; }
    public string SuggestionType { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
}

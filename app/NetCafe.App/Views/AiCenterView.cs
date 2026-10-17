// Trách nhiệm: bảng điều khiển 5 AI PL/pgSQL, không gọi API/LLM bên ngoài.
// Liên kết: sql/07_ai_modules.sql và notification channel ai_security_alert.

namespace NetCafe.App.Views;

public partial class AiCenterView : AdminDataViewBase
{
    private readonly Button _runAllButton = new();

    public AiCenterView()
    {
        InitializeComponent();
        SetTitle("AI Center", "Security, index, duplicate, anomaly và promotion đều chạy trong PostgreSQL.");
        _runAllButton.Text = "Chạy 5 module";
        _runAllButton.Width = 120;
        _runAllButton.BackColor = PrimaryColor;
        _runAllButton.ForeColor = Color.White;
        _runAllButton.FlatStyle = FlatStyle.Flat;
        _runAllButton.FlatAppearance.BorderSize = 0;
        _runAllButton.Click += async (_, _) => await RefreshAsync();
        Toolbar.Controls.Add(_runAllButton);
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await RequiredContext.Repository.GetAiAlertsAsync(cancellationToken);
        var indexes = await RequiredContext.Repository.RecommendIndexesAsync(cancellationToken);
        var duplicates = await RequiredContext.Repository.DetectDuplicatesAsync(cancellationToken);
        var anomalies = await RequiredContext.Repository.DetectAnomaliesAsync(cancellationToken);
        var promotions = await RequiredContext.Repository.SuggestPromotionsAsync(cancellationToken);

        var rows = new List<AiModuleRow>();
        rows.AddRange(alerts.Select(item => new AiModuleRow("Security Alert", item.AlertId.ToString(), item.Reason, item.Status)));
        rows.AddRange(indexes.Select(item => new AiModuleRow("Index Recommendation", item.RecommendationId.ToString(), item.SuggestedSql, "readonly")));
        rows.AddRange(duplicates.Select(item => new AiModuleRow("Duplicate Customer", item.CandidateId.ToString(), item.Reason, "review")));
        rows.AddRange(anomalies.Select(item => new AiModuleRow("Anomaly Detection", item.FlagId.ToString(), $"{item.AnomalyType}: z={item.ZScore}", item.Status)));
        rows.AddRange(promotions.Select(item => new AiModuleRow("Promotion Suggestion", item.SuggestionId.ToString(), item.Details, item.SuggestionType)));
        SetRows(rows, $"5 module PostgreSQL: {alerts.Count} alert, {indexes.Count} index, {duplicates.Count} duplicate, {anomalies.Count} anomaly, {promotions.Count} promotion.");
    }

    private sealed record AiModuleRow(string Module, string Key, string Summary, string Status);
}

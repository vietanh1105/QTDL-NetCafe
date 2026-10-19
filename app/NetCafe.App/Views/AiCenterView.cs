// Trách nhiệm: bảng điều khiển 5 AI PL/pgSQL, không gọi API/LLM bên ngoài.
// Liên kết: sql/07_ai_modules.sql và notification channel ai_security_alert.

namespace NetCafe.App.Views;

public partial class AiCenterView : AdminDataViewBase
{
    private readonly Button _runAllButton = new();
    private readonly Button _runIndexButton = new();
    private readonly Button _runDuplicateButton = new();
    private readonly Button _runAnomalyButton = new();
    private readonly Button _runPromotionButton = new();
    private readonly Button _reviewAlertButton = new();
    private readonly Button _mergeDuplicateButton = new();
    private readonly Button _ignoreDuplicateButton = new();

    public AiCenterView()
    {
        InitializeComponent();
        SetTitle("Trung tâm AI & Tự động hóa", "Bảo mật, chỉ mục, phát hiện trùng lặp, dị thường và khuyến mãi chạy trực tiếp trong CSDL PostgreSQL.");
        _runAllButton.Text = "Chạy cả 5 mô-đun";
        _runAllButton.Width = 135;
        _runAllButton.BackColor = PrimaryColor;
        _runAllButton.ForeColor = Color.White;
        _runAllButton.FlatStyle = FlatStyle.Flat;
        _runAllButton.FlatAppearance.BorderSize = 0;
        _runAllButton.Click += async (_, _) => await RunAllAsync();
        ConfigureButton(_runIndexButton, "Tối ưu chỉ mục");
        ConfigureButton(_runDuplicateButton, "Tìm trùng lặp");
        ConfigureButton(_runAnomalyButton, "Phát hiện dị thường");
        ConfigureButton(_runPromotionButton, "Gợi ý khuyến mãi");
        ConfigureButton(_reviewAlertButton, "Đã xem cảnh báo");
        ConfigureButton(_mergeDuplicateButton, "Đánh dấu đã gộp");
        ConfigureButton(_ignoreDuplicateButton, "Đánh dấu bỏ qua");
        _runIndexButton.Click += async (_, _) => await RunModuleAsync("index");
        _runDuplicateButton.Click += async (_, _) => await RunModuleAsync("duplicate");
        _runAnomalyButton.Click += async (_, _) => await RunModuleAsync("anomaly");
        _runPromotionButton.Click += async (_, _) => await RunModuleAsync("promotion");
        _reviewAlertButton.Click += async (_, _) => await UpdateSelectedAsync("reviewed");
        _mergeDuplicateButton.Click += async (_, _) => await UpdateSelectedAsync("merged");
        _ignoreDuplicateButton.Click += async (_, _) => await UpdateSelectedAsync("ignored");
        Toolbar.Controls.Add(_ignoreDuplicateButton);
        Toolbar.Controls.Add(_mergeDuplicateButton);
        Toolbar.Controls.Add(_reviewAlertButton);
        Toolbar.Controls.Add(_runPromotionButton);
        Toolbar.Controls.Add(_runAnomalyButton);
        Toolbar.Controls.Add(_runDuplicateButton);
        Toolbar.Controls.Add(_runIndexButton);
        Toolbar.Controls.Add(_runAllButton);
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await RequiredContext.Repository.GetAiAlertsAsync(cancellationToken);
        var indexes = await RequiredContext.Repository.GetIndexRecommendationsAsync(cancellationToken);
        var duplicates = await RequiredContext.Repository.GetDuplicateCandidatesAsync(cancellationToken);
        var anomalies = await RequiredContext.Repository.GetAnomalyFlagsAsync(cancellationToken);
        var promotions = await RequiredContext.Repository.GetPromotionSuggestionsAsync(cancellationToken);

        var rows = new List<AiModuleRow>();
        rows.AddRange(alerts.Select(item => new AiModuleRow("Cảnh báo bảo mật", item.AlertId.ToString(), item.Reason, item.Status, item.AlertId, null)));
        rows.AddRange(indexes.Select(item => new AiModuleRow("Đề xuất chỉ mục", item.RecommendationId.ToString(), item.SuggestedSql, "Chỉ đọc", null, null)));
        rows.AddRange(duplicates.Select(item => new AiModuleRow("Hội viên trùng lặp", item.CandidateId.ToString(), item.Reason, item.Status, null, item.CandidateId)));
        rows.AddRange(anomalies.Select(item => new AiModuleRow("Phát hiện dị thường", item.FlagId.ToString(), $"{item.AnomalyType}: z={item.ZScore}", item.Status, null, null)));
        rows.AddRange(promotions.Select(item => new AiModuleRow("Gợi ý khuyến mãi", item.SuggestionId.ToString(), item.Details, item.SuggestionType, null, null)));
        SetRows(rows, $"5 mô-đun PostgreSQL: {alerts.Count} cảnh báo, {indexes.Count} chỉ mục, {duplicates.Count} trùng lặp, {anomalies.Count} dị thường, {promotions.Count} khuyến mãi.");
    }

    private async Task RunAllAsync()
    {
        try
        {
            await RequiredContext.Repository.RunAiModulesAsync();
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task RunModuleAsync(string module)
    {
        try
        {
            await RequiredContext.Repository.RunAiModuleAsync(module);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task UpdateSelectedAsync(string status)
    {
        if (Grid.CurrentRow?.DataBoundItem is not AiModuleRow row)
        {
            return;
        }

        try
        {
            if (row.AlertId is not null)
            {
                await RequiredContext.Repository.UpdateAiAlertStatusAsync(row.AlertId.Value, status == "reviewed" ? "reviewed" : "new");
            }
            else if (row.CandidateId is not null)
            {
                await RequiredContext.Repository.UpdateDuplicateCandidateStatusAsync(row.CandidateId.Value, status);
            }
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Width = 108;
        button.BackColor = Color.FromArgb(71, 85, 105);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
    }

    private sealed record AiModuleRow(
        string Module,
        string Key,
        string Summary,
        string Status,
        int? AlertId,
        int? CandidateId);
}

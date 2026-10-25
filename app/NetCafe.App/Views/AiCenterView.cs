// Trách nhiệm: bảng điều khiển 5 AI PL/pgSQL, không gọi API/LLM bên ngoài.
// Liên kết: sql/07_ai_modules.sql và notification channel ai_security_alert.

namespace NetCafe.App.Views;

public partial class AiCenterView : AdminCardViewBase
{
    private readonly Button _runAllButton = new();
    private readonly Button _runIndexButton = new();
    private readonly Button _runDuplicateButton = new();
    private readonly Button _runAnomalyButton = new();
    private readonly Button _runPromotionButton = new();
    private readonly Button _runSecurityButton = new();
    private readonly Button _reviewAlertButton = new();
    private readonly Button _mergeDuplicateButton = new();
    private readonly Button _ignoreDuplicateButton = new();

    public AiCenterView()
    {
        InitializeComponent();
        SetTitle(
            "Phân tích và cảnh báo dữ liệu",
            "Năm mô-đun PL/pgSQL chạy trong PostgreSQL; mỗi card có trạng thái, mô tả và nút xử lý riêng.");

        _runAllButton.Text = "Chạy cả 5 mô-đun";
        _runAllButton.Width = 135;
        CyberTheme.StyleButton(_runAllButton, PrimaryColor);
        _runAllButton.Click += async (_, _) => await RunAllAsync();

        ConfigureButton(_runIndexButton, "Tối ưu chỉ mục");
        ConfigureButton(_runDuplicateButton, "Tìm trùng lặp");
        ConfigureButton(_runAnomalyButton, "Phát hiện dị thường");
        ConfigureButton(_runPromotionButton, "Gợi ý khuyến mãi");
        ConfigureButton(_runSecurityButton, "Kiểm tra bảo mật");
        ConfigureButton(_reviewAlertButton, "Đã xem cảnh báo");
        ConfigureButton(_mergeDuplicateButton, "Đánh dấu đã gộp");
        ConfigureButton(_ignoreDuplicateButton, "Đánh dấu bỏ qua");

        _runIndexButton.Click += async (_, _) => await RunModuleAsync("index");
        _runDuplicateButton.Click += async (_, _) => await RunModuleAsync("duplicate");
        _runAnomalyButton.Click += async (_, _) => await RunModuleAsync("anomaly");
        _runPromotionButton.Click += async (_, _) => await RunModuleAsync("promotion");
        _runSecurityButton.Click += async (_, _) => await RunModuleAsync("security");
        _reviewAlertButton.Click += async (_, _) => await UpdateSelectedAsync("reviewed");
        _mergeDuplicateButton.Click += async (_, _) => await UpdateSelectedAsync("merged");
        _ignoreDuplicateButton.Click += async (_, _) => await UpdateSelectedAsync("ignored");

        ActionsPanel.Controls.Add(_runAllButton);
        ActionsPanel.Controls.Add(_runIndexButton);
        ActionsPanel.Controls.Add(_runDuplicateButton);
        ActionsPanel.Controls.Add(_runAnomalyButton);
        ActionsPanel.Controls.Add(_runPromotionButton);
        ActionsPanel.Controls.Add(_runSecurityButton);
        ActionsPanel.Controls.Add(_reviewAlertButton);
        ActionsPanel.Controls.Add(_mergeDuplicateButton);
        ActionsPanel.Controls.Add(_ignoreDuplicateButton);
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        var canAccess = context.IsAdmin || context.IsManager;
        _runAllButton.Enabled = canAccess;
        _runIndexButton.Enabled = canAccess;
        _runDuplicateButton.Enabled = canAccess;
        _runAnomalyButton.Enabled = canAccess;
        _runPromotionButton.Enabled = canAccess;
        _runSecurityButton.Enabled = canAccess;
        _reviewAlertButton.Enabled = canAccess;
        _mergeDuplicateButton.Enabled = canAccess;
        _ignoreDuplicateButton.Enabled = canAccess;

        if (!canAccess)
        {
            ClearCards("🔒 Vai trò hiện tại không có quyền truy cập AI Center (Cơ chế CSDL: Schema Grant ai).");
        }
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (RequiredContext.IsCashier)
        {
            UpdateRolePermissions(RequiredContext);
            return;
        }

        try
        {
            var alerts = await RequiredContext.Repository.GetAiAlertsAsync(cancellationToken);
            var indexes = await RequiredContext.Repository.GetIndexRecommendationsAsync(cancellationToken);
            var duplicates = await RequiredContext.Repository.GetDuplicateCandidatesAsync(cancellationToken);
            var anomalies = await RequiredContext.Repository.GetAnomalyFlagsAsync(cancellationToken);
            var promotions = await RequiredContext.Repository.GetPromotionSuggestionsAsync(cancellationToken);

            var rows = new List<AiModuleRow>();
            rows.AddRange(alerts.Select(item =>
                new AiModuleRow("Cảnh báo bảo mật", item.AlertId.ToString(), item.Reason, item.Status, item.AlertId, null)));
            rows.AddRange(indexes.Select(item =>
                new AiModuleRow("Đề xuất chỉ mục", item.RecommendationId.ToString(), item.SuggestedSql, "Chỉ đọc", null, null)));
            rows.AddRange(duplicates.Select(item =>
                new AiModuleRow("Hội viên trùng lặp", item.CandidateId.ToString(), item.Reason, item.Status, null, item.CandidateId)));
            rows.AddRange(anomalies.Select(item =>
                new AiModuleRow("Phát hiện dị thường", item.FlagId.ToString(), $"{item.AnomalyType}: z={item.ZScore}", item.Status, null, null)));
            rows.AddRange(promotions.Select(item =>
                new AiModuleRow("Gợi ý khuyến mãi", item.SuggestionId.ToString(), item.Details, item.SuggestionType, null, null)));

            var summary = $"5 mô-đun PostgreSQL: {alerts.Count} cảnh báo, {indexes.Count} chỉ mục, {duplicates.Count} trùng lặp, {anomalies.Count} dị thường, {promotions.Count} khuyến mãi.";
            SetCards(rows, BuildCard, summary);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
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
        if (SelectedItem is not AiModuleRow row)
        {
            return;
        }

        try
        {
            if (row.AlertId is not null)
            {
                await RequiredContext.Repository.UpdateAiAlertStatusAsync(
                    row.AlertId.Value, status == "reviewed" ? "reviewed" : "new");
            }
            else if (row.CandidateId is not null)
            {
                await RequiredContext.Repository.UpdateDuplicateCandidateStatusAsync(
                    row.CandidateId.Value, status);
            }
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private static AdminCardModel BuildCard(AiModuleRow row)
    {
        var accent = row.Status.ToLowerInvariant() switch
        {
            "new" => CyberTheme.Warning,
            "reviewed" or "merged" or "ignored" => CyberTheme.Success,
            "chỉ đọc" => CyberTheme.Primary,
            _ => CyberTheme.MutedText
        };
        return new AdminCardModel(
            $"{row.Module} · {row.Key}",
            "Phân tích dữ liệu PostgreSQL",
            row.Summary,
            row.Status,
            accent);
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Width = 108;
        CyberTheme.StyleButton(button, CyberTheme.SurfaceRaised);
    }

    private sealed record AiModuleRow(
        string Module,
        string Key,
        string Summary,
        string Status,
        int? AlertId,
        int? CandidateId);
}

// Trách nhiệm: bảng điều khiển 5 module AI PL/pgSQL với giao diện phân loại ngăn nắp.
// Liên kết: sql/07_ai_modules.sql và notification channel ai_security_alert.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class AiCenterView : AdminCardViewBase
{
    private string _currentModule = "security";

    // 5 nút chuyển đổi danh mục mô-đun AI
    private readonly Button _securityTabButton = new();
    private readonly Button _duplicateTabButton = new();
    private readonly Button _anomalyTabButton = new();
    private readonly Button _promotionTabButton = new();
    private readonly Button _indexTabButton = new();

    // Các nút thao tác ngữ cảnh
    private readonly Button _runModuleButton = new();
    private readonly Button _reviewAlertButton = new();
    private readonly Button _mergeDuplicateButton = new();
    private readonly Button _ignoreDuplicateButton = new();

    public AiCenterView()
    {
        InitializeComponent();
        Toolbar.Height = 88;
        TitleLabel.Width = 170;
        ActionsPanel.Padding = new Padding(2, 2, 2, 2);
        ConfigureControls();
        SwitchModule("security");
    }

    private void ConfigureControls()
    {
        ConfigureTabButton(_securityTabButton, "🛡️ Gian lận", "security", 125);
        ConfigureTabButton(_duplicateTabButton, "👥 Trùng lặp", "duplicate", 125);
        ConfigureTabButton(_anomalyTabButton, "📈 Dị thường", "anomaly", 125);
        ConfigureTabButton(_promotionTabButton, "💡 Khuyến mãi", "promotion", 125);
        ConfigureTabButton(_indexTabButton, "⚡ Tối ưu CSDL", "index", 125);

        ConfigureActionButton(_runModuleButton, "▶️ Quét an ninh", CyberTheme.Primary, 135);
        ConfigureActionButton(_reviewAlertButton, "✅ Đã xử lý", CyberTheme.Success, 110);
        ConfigureActionButton(_mergeDuplicateButton, "🔗 Đã gộp", CyberTheme.Success, 105);
        ConfigureActionButton(_ignoreDuplicateButton, "✖️ Bỏ qua", CyberTheme.SurfaceRaised, 85);

        _runModuleButton.Click += async (_, _) => await RunCurrentModuleAsync();
        _reviewAlertButton.Click += async (_, _) => await UpdateSecurityAlertStatusAsync("reviewed");
        _mergeDuplicateButton.Click += async (_, _) => await UpdateDuplicateStatusAsync("merged");
        _ignoreDuplicateButton.Click += async (_, _) => await UpdateDuplicateStatusAsync("ignored");

        ActionsPanel.Controls.Add(_securityTabButton);
        ActionsPanel.Controls.Add(_duplicateTabButton);
        ActionsPanel.Controls.Add(_anomalyTabButton);
        ActionsPanel.Controls.Add(_promotionTabButton);
        ActionsPanel.Controls.Add(_indexTabButton);
        ActionsPanel.SetFlowBreak(_indexTabButton, true);

        ActionsPanel.Controls.Add(_runModuleButton);
        ActionsPanel.Controls.Add(_reviewAlertButton);
        ActionsPanel.Controls.Add(_mergeDuplicateButton);
        ActionsPanel.Controls.Add(_ignoreDuplicateButton);
    }

    private void ConfigureTabButton(Button button, string text, string module, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 30;
        button.Margin = new Padding(2, 2, 2, 2);
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.FlatStyle = FlatStyle.Flat;
        button.Click += (_, _) => SwitchModule(module);
    }

    private static void ConfigureActionButton(Button button, string text, Color color, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 30;
        button.Margin = new Padding(2, 2, 2, 2);
        CyberTheme.StyleButton(button, color);
    }

    private void SwitchModule(string module)
    {
        _currentModule = module;
        UpdateModuleTabStyles();
        UpdateActionControlsVisibility();
        UpdateModuleTitle();
        _ = RefreshAsync();
    }

    private void UpdateModuleTabStyles()
    {
        UpdateButtonState(_securityTabButton, _currentModule == "security");
        UpdateButtonState(_duplicateTabButton, _currentModule == "duplicate");
        UpdateButtonState(_anomalyTabButton, _currentModule == "anomaly");
        UpdateButtonState(_promotionTabButton, _currentModule == "promotion");
        UpdateButtonState(_indexTabButton, _currentModule == "index");
    }

    private static void UpdateButtonState(Button button, bool isActive)
    {
        button.BackColor = isActive ? CyberTheme.Primary : CyberTheme.SurfaceRaised;
        button.ForeColor = isActive ? Color.White : CyberTheme.Text;
        button.FlatAppearance.BorderColor = isActive ? CyberTheme.Primary : CyberTheme.Border;
    }

    private void UpdateActionControlsVisibility()
    {
        _runModuleButton.Text = _currentModule switch
        {
            "security" => "▶️ Quét an ninh",
            "duplicate" => "▶️ Quét trùng lặp",
            "anomaly" => "▶️ Quét dị thường",
            "promotion" => "▶️ Quét khuyến mãi",
            _ => "▶️ Quét hiệu năng CSDL"
        };

        _reviewAlertButton.Visible = _currentModule == "security";
        _mergeDuplicateButton.Visible = _currentModule == "duplicate";
        _ignoreDuplicateButton.Visible = _currentModule == "duplicate";
    }

    private void UpdateModuleTitle()
    {
        switch (_currentModule)
        {
            case "duplicate":
                SetTitle(
                    "Trùng lặp hội viên",
                    "Thuật toán pg_trgm so khớp chuỗi phát hiện các tài khoản nghi vấn trùng lặp để gộp quyền lợi.");
                break;
            case "anomaly":
                SetTitle(
                    "Giao dịch dị thường",
                    "Mô hình thống kê Z-Score phát hiện các khoản nạp tiền hoặc phiên chơi đột biến bất thường.");
                break;
            case "promotion":
                SetTitle(
                    "Gợi ý khuyến mãi",
                    "Phân tích hành vi khách hàng RFM và khung giờ thấp điểm để đề xuất chương trình kích cầu.");
                break;
            case "index":
                SetTitle(
                    "Tối ưu tốc độ CSDL",
                    "Tự động rà soát các bảng dữ liệu lớn bị chậm và hướng dẫn tạo chỉ mục (Index) tăng tốc.");
                break;
            default:
                SetTitle(
                    "An ninh phòng máy",
                    "Tự động giám sát các hành vi hủy hóa đơn, sửa giờ chơi và truy cập bất thường.");
                break;
        }
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        var canAccess = context.IsAdmin || context.IsManager;
        _runModuleButton.Enabled = canAccess;
        _reviewAlertButton.Enabled = canAccess;
        _mergeDuplicateButton.Enabled = canAccess;
        _ignoreDuplicateButton.Enabled = canAccess;

        if (!canAccess)
        {
            ClearCards("🔒 Chỉ Chủ quán và Quản lý mới có quyền truy cập AI Center (Cơ chế CSDL: Schema Grant ai).");
        }
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!RequiredContext.IsAdmin && !RequiredContext.IsManager)
        {
            UpdateRolePermissions(RequiredContext);
            return;
        }

        try
        {
            switch (_currentModule)
            {
                case "duplicate":
                    await LoadDuplicatesAsync(cancellationToken);
                    break;
                case "anomaly":
                    await LoadAnomaliesAsync(cancellationToken);
                    break;
                case "promotion":
                    await LoadPromotionsAsync(cancellationToken);
                    break;
                case "index":
                    await LoadIndexesAsync(cancellationToken);
                    break;
                default:
                    await LoadSecurityAlertsAsync(cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private async Task LoadSecurityAlertsAsync(CancellationToken cancellationToken)
    {
        var alerts = await RequiredContext.Repository.GetAiAlertsAsync(cancellationToken);
        if (alerts.Count == 0)
        {
            ClearCards("✅ Không phát hiện cảnh báo rủi ro an ninh nào. Hệ thống an toàn!");
            return;
        }

        SetCards(alerts, alert => new AdminCardModel(
            $"🛡️ Cảnh báo #{alert.AlertId} • Rủi ro: {alert.RiskLevel.ToUpper()}",
            $"Điểm rủi ro: {alert.RiskScore}/100 • Ngày: {alert.CreatedAt:dd/MM/yyyy HH:mm}",
            CleanNoParentheses(alert.Reason),
            alert.Status == "reviewed" ? "Đã xử lý" : "Mới phát hiện",
            alert.Status == "reviewed" ? CyberTheme.Success : CyberTheme.Danger),
            $"{alerts.Count} cảnh báo an ninh được ghi nhận");
    }

    private async Task LoadDuplicatesAsync(CancellationToken cancellationToken)
    {
        var duplicates = await RequiredContext.Repository.GetDuplicateCandidatesAsync(cancellationToken);
        if (duplicates.Count == 0)
        {
            ClearCards("✅ Không phát hiện tài khoản hội viên trùng lặp nào.");
            return;
        }

        SetCards(duplicates, item => new AdminCardModel(
            $"👥 Nghi vấn: {item.CustomerNameA} & {item.CustomerNameB}",
            $"Tương đồng: {(int)(item.Similarity * 100)}% • Khách #{item.CustomerIdA} & #{item.CustomerIdB}",
            $"Lý do: {CleanNoParentheses(item.Reason)}",
            item.Status == "merged" ? "Đã gộp" : (item.Status == "ignored" ? "Đã bỏ qua" : "Chờ xử lý"),
            item.Status == "merged" ? CyberTheme.Success : (item.Status == "ignored" ? CyberTheme.MutedText : CyberTheme.Warning)),
            $"{duplicates.Count} cặp hội viên nghi vấn trùng lặp");
    }

    private async Task LoadAnomaliesAsync(CancellationToken cancellationToken)
    {
        var anomalies = await RequiredContext.Repository.GetAnomalyFlagsAsync(cancellationToken);
        if (anomalies.Count == 0)
        {
            ClearCards("✅ Toàn bộ số tiền nạp và thời lượng chơi đều ở ngưỡng thống kê bình thường.");
            return;
        }

        SetCards(anomalies, item => new AdminCardModel(
            $"📈 Dị thường #{item.FlagId} • Bảng: {item.SubjectTable}",
            $"Dạng: {item.AnomalyType} • Khóa chính: #{item.SubjectPk}",
            $"Giá trị: {item.MetricValue:N0} đ • Độ lệch chuẩn Z-Score: {item.ZScore:F2}\nLý do: Giao dịch có chênh lệch lớn so với mức trung bình",
            item.Status == "investigated" ? "Đã xác minh" : "Cần kiểm tra",
            CyberTheme.Warning),
            $"{anomalies.Count} giao dịch có dấu hiệu bất thường");
    }

    private async Task LoadPromotionsAsync(CancellationToken cancellationToken)
    {
        var promotions = await RequiredContext.Repository.GetPromotionSuggestionsAsync(cancellationToken);
        if (promotions.Count == 0)
        {
            ClearCards("ℹ️ Bấm '▶️ Quét khuyến mãi' để hệ thống phân tích và đề xuất gói ưu đãi.");
            return;
        }

        SetCards(promotions, item => new AdminCardModel(
            $"💡 Ưu đãi đề xuất #{item.SuggestionId}",
            $"Mục tiêu: {CleanNoParentheses(item.Target)} • Mô hình: {FormatPromoType(item.SuggestionType)}",
            FormatPromoDetails(item.Details),
            "Gợi ý kinh doanh",
            CyberTheme.Primary),
            $"{promotions.Count} gợi ý khuyến mãi thông minh");
    }

    private async Task LoadIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = await RequiredContext.Repository.GetIndexRecommendationsAsync(cancellationToken);
        if (indexes.Count == 0)
        {
            ClearCards("✅ Tất cả các bảng dữ liệu hiện tại đều đã được đánh chỉ mục và tối ưu hóa tốt!");
            return;
        }

        SetCards(indexes, item => new AdminCardModel(
            $"⚡ Tăng tốc bảng: {item.TableName}",
            $"Cột đề xuất mục lục: {CleanNoParentheses(item.ColumnList)}",
            $"Mục đích: {CleanNoParentheses(item.Reason)}\nLệnh SQL thực thi: {CleanNoParentheses(item.SuggestedSql)}",
            "Đề xuất tăng tốc",
            CyberTheme.Primary),
            $"{indexes.Count} khuyến nghị tối ưu hóa chỉ mục CSDL");
    }

    private static string CleanNoParentheses(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var result = System.Text.RegularExpressions.Regex.Replace(input, @"\s*\(?ID\s*\d+\)?", "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return result.Replace("(", " ")
                    .Replace(")", " ")
                    .Replace("[", " ")
                    .Replace("]", " ")
                    .Replace("{", " ")
                    .Replace("}", " ")
                    .Replace("\"", "")
                    .Replace("  ", " ")
                    .Trim();
    }

    private static string FormatPromoDetails(string? rawDetails)
    {
        if (string.IsNullOrWhiteSpace(rawDetails)) return string.Empty;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawDetails);
            var root = doc.RootElement;
            var parts = new List<string>();

            if (root.TryGetProperty("recommended_action", out var action))
            {
                parts.Add($"Đề xuất: {action.GetString()}");
            }
            if (root.TryGetProperty("rationale", out var rationale))
            {
                parts.Add($"Lý do: {rationale.GetString()}");
            }
            if (root.TryGetProperty("hour", out var hour))
            {
                parts.Add($"Khung giờ áp dụng: {hour.GetInt32()}h00");
            }
            if (root.TryGetProperty("total_spent", out var spent))
            {
                parts.Add($"Tổng chi tiêu tích lũy: {spent.GetDecimal():N0} đ");
            }
            if (root.TryGetProperty("days_inactive", out var days))
            {
                parts.Add($"Thời gian chưa quay lại: {days} ngày");
            }
            if (root.TryGetProperty("current_sessions", out var curr))
            {
                parts.Add($"Lượt khách hiện tại: {curr}");
            }

            if (parts.Count > 0)
            {
                return CleanNoParentheses(string.Join("\n", parts));
            }
        }
        catch
        {
            // fallback
        }

        return CleanNoParentheses(rawDetails);
    }

    private static string FormatPromoType(string type) => type switch
    {
        "rfm" or "WIN_BACK_VIP" => "Chăm sóc hội viên VIP",
        "low_utilization" or "HAPPY_HOUR_OFFPEAK" => "Kích cầu giờ thấp điểm",
        _ => CleanNoParentheses(type)
    };

    private async Task RunCurrentModuleAsync()
    {
        try
        {
            await RequiredContext.Repository.RunAiModuleAsync(_currentModule);
            await RefreshAsync();
            InfoLabel.Text = $"Đã chạy xong mô-đun {_currentModule} lúc {DateTime.Now:HH:mm:ss}!";
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task UpdateSecurityAlertStatusAsync(string status)
    {
        if (SelectedItem is not AiAlertItem alert)
        {
            MessageBox.Show(FindForm(), "Vui lòng bấm chọn một cảnh báo an ninh trong danh sách để cập nhật.",
                "Chưa chọn cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            await RequiredContext.Repository.UpdateAiAlertStatusAsync(alert.AlertId, status);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task UpdateDuplicateStatusAsync(string status)
    {
        if (SelectedItem is not DuplicateCandidateItem dup)
        {
            MessageBox.Show(FindForm(), "Vui lòng bấm chọn một cặp hội viên trong danh sách để xử lý.",
                "Chưa chọn tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            await RequiredContext.Repository.UpdateDuplicateCandidateStatusAsync(dup.CandidateId, status);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}

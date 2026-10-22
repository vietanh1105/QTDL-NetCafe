// Trách nhiệm: dashboard báo cáo theo từng nhóm dữ liệu, refresh materialized view và export.
// Liên kết: reporting views/materialized views, DbRepository và ExportService.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Services;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class ReportsView : AdminDataViewBase
{
    private readonly Button _revenueButton = new();
    private readonly Button _usageButton = new();
    private readonly Button _customersButton = new();
    private readonly Button _refreshSnapshotsButton = new();
    private readonly Button _exportPdfButton = new();
    private readonly Button _exportExcelButton = new();
    private readonly Label _fromLabel = new() { Text = "Từ:", AutoSize = true, Padding = new Padding(0, 8, 2, 0) };
    private readonly DateTimePicker _fromPicker = new();
    private readonly Label _toLabel = new() { Text = "Đến:", AutoSize = true, Padding = new Padding(4, 8, 2, 0) };
    private readonly DateTimePicker _toPicker = new();
    private string _mode = "revenue";

    public ReportsView()
    {
        InitializeComponent();
        SetTitle("Báo cáo vận hành", "Chọn một nhóm báo cáo; dữ liệu snapshot chỉ refresh khi bấm Cập nhật.");
        ConfigureToolbar();
    }

    private void ConfigureToolbar()
    {
        ConfigureModeButton(_revenueButton, "📅 Doanh thu theo ngày", "revenue");
        ConfigureModeButton(_usageButton, "🖥️ Hiệu suất máy trạm", "usage");
        ConfigureModeButton(_customersButton, "👑 Top Hội viên chi tiêu", "customers");

        _fromPicker.Format = DateTimePickerFormat.Short;
        _fromPicker.Width = 95;
        _fromPicker.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _toPicker.Format = DateTimePickerFormat.Short;
        _toPicker.Width = 95;
        _toPicker.Value = DateTime.Today;
        _fromPicker.ValueChanged += async (_, _) => await RefreshAsync();
        _toPicker.ValueChanged += async (_, _) => await RefreshAsync();

        ConfigureToolbarButton(_refreshSnapshotsButton, "⚡ Cập nhật số liệu (REFRESH MATVIEW)", CyberTheme.Warning, 240);
        ConfigureToolbarButton(_exportPdfButton, "📄 PDF", CyberTheme.Primary, 72);
        ConfigureToolbarButton(_exportExcelButton, "📊 Excel", CyberTheme.Success, 78);

        _refreshSnapshotsButton.Click += async (_, _) => await RefreshSnapshotsAsync();
        _exportPdfButton.Click += async (_, _) => await ExportPdfAsync();
        _exportExcelButton.Click += async (_, _) => await ExportExcelAsync();

        ActionsPanel.Controls.Add(_revenueButton);
        ActionsPanel.Controls.Add(_usageButton);
        ActionsPanel.Controls.Add(_customersButton);
        ActionsPanel.Controls.Add(_fromLabel);
        ActionsPanel.Controls.Add(_fromPicker);
        ActionsPanel.Controls.Add(_toLabel);
        ActionsPanel.Controls.Add(_toPicker);
        ActionsPanel.Controls.Add(_refreshSnapshotsButton);
        ActionsPanel.Controls.Add(_exportPdfButton);
        ActionsPanel.Controls.Add(_exportExcelButton);

        UpdateModeButtonStyles();
    }

    private void UpdateModeButtonStyles()
    {
        _revenueButton.BackColor = _mode == "revenue" ? CyberTheme.Primary : CyberTheme.SurfaceRaised;
        _revenueButton.ForeColor = _mode == "revenue" ? Color.White : CyberTheme.Text;
        _usageButton.BackColor = _mode == "usage" ? CyberTheme.Primary : CyberTheme.SurfaceRaised;
        _usageButton.ForeColor = _mode == "usage" ? Color.White : CyberTheme.Text;
        _customersButton.BackColor = _mode == "customers" ? CyberTheme.Primary : CyberTheme.SurfaceRaised;
        _customersButton.ForeColor = _mode == "customers" ? Color.White : CyberTheme.Text;

        var isRevenue = _mode == "revenue";
        _fromLabel.Visible = isRevenue;
        _fromPicker.Visible = isRevenue;
        _toLabel.Visible = isRevenue;
        _toPicker.Visible = isRevenue;
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            UpdateModeButtonStyles();
            switch (_mode)
            {
                case "usage":
                {
                    var rows = await RequiredContext.Repository.GetComputerUsageAsync(cancellationToken);
                    SetRows(rows, $"Hiệu suất {rows.Count} máy trạm | Tỷ lệ lấp đầy tính trên chuẩn 720 giờ/tháng");
                    break;
                }
                case "customers":
                {
                    var rows = await RequiredContext.Repository.GetTopCustomersAsync(cancellationToken);
                    SetRows(rows, $"Top {rows.Count} hội viên theo chi tiêu | Materialized View Snapshot");
                    break;
                }
                default:
                {
                    var rows = await RequiredContext.Repository.GetDailyRevenueAsync(cancellationToken);
                    var from = _fromPicker.Value.Date;
                    var to = _toPicker.Value.Date;
                    rows = rows.Where(row => row.RevenueDate.Date >= from && row.RevenueDate.Date <= to).ToList();
                    SetRows(rows, $"Doanh thu {rows.Count} ngày | {_fromPicker.Value:dd/MM/yyyy} → {_toPicker.Value:dd/MM/yyyy}");
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private void ConfigureModeButton(Button button, string text, string mode)
    {
        button.Text = text;
        button.Width = text.Length > 20 ? 175 : 155;
        button.Height = 32;
        button.Margin = new Padding(3, 2, 3, 2);
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Click += async (_, _) =>
        {
            _mode = mode;
            await RefreshAsync();
        };
    }

    private static void ConfigureToolbarButton(Button button, string text, Color color, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 32;
        button.Margin = new Padding(3, 2, 3, 2);
        CyberTheme.StyleButton(button, color);
    }

    private async Task RefreshSnapshotsAsync()
    {
        try
        {
            await RequiredContext.Repository.RefreshMaterializedViewsAsync();
            await RefreshAsync();
            MessageBox.Show(FindForm(),
                "Đã chạy lệnh REFRESH MATERIALIZED VIEW (mv_hourly_usage, mv_top_customers, mv_monthly_revenue) thành công!\nDữ liệu báo cáo đã được làm mới.",
                "Cập nhật số liệu thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task ExportExcelAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"bao-cao-{_mode}-{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            if (_mode == "usage")
            {
                var rows = await RequiredContext.Repository.GetComputerUsageAsync();
                ExportService.ExportObjectsExcel(rows, "Hiệu suất máy", dialog.FileName);
            }
            else if (_mode == "customers")
            {
                var rows = await RequiredContext.Repository.GetTopCustomersAsync();
                ExportService.ExportObjectsExcel(rows, "Top hội viên", dialog.FileName);
            }
            else
            {
                var rows = await RequiredContext.Repository.GetDailyRevenueAsync();
                var filtered = rows.Where(row => row.RevenueDate.Date >= _fromPicker.Value.Date && row.RevenueDate.Date <= _toPicker.Value.Date);
                ExportService.ExportObjectsExcel(filtered, "Doanh thu ngày", dialog.FileName);
            }
            MessageBox.Show(FindForm(), $"Đã xuất Excel: {dialog.FileName}", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task ExportPdfAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"bao-cao-{_mode}-{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var lines = new List<string> { $"Khoảng ngày: {_fromPicker.Value:dd/MM/yyyy} → {_toPicker.Value:dd/MM/yyyy}" };
            if (_mode == "usage")
            {
                var rows = await RequiredContext.Repository.GetComputerUsageAsync();
                lines.AddRange(rows.Select(row => $"{row.ComputerCode} ({row.ComputerName}): {row.TotalHoursPlayed:N1} giờ | Lấp đầy: {row.OccupancyRate:N1}% | Thu: {row.TotalPlayRevenue:N0} đ"));
            }
            else if (_mode == "customers")
            {
                var rows = await RequiredContext.Repository.GetTopCustomersAsync();
                lines.AddRange(rows.Select(row => $"{row.FullName} ({row.MembershipTier}): {row.TotalSpent:N0} đ | {row.TotalSessions} phiên"));
            }
            else
            {
                var rows = await RequiredContext.Repository.GetDailyRevenueAsync();
                lines.AddRange(rows.Where(row => row.RevenueDate.Date >= _fromPicker.Value.Date && row.RevenueDate.Date <= _toPicker.Value.Date)
                    .Select(row => $"{row.RevenueDate:dd/MM/yyyy}: Giờ={row.TotalPlayAmount:N0} đ | Dịch vụ={row.TotalServiceAmount:N0} đ | Ròng={row.NetRevenue:N0} đ"));
            }
            ExportService.ExportReportPdf($"Báo cáo NetCafe - {_mode}", lines, dialog.FileName);
            MessageBox.Show(FindForm(), $"Đã xuất PDF: {dialog.FileName}", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}

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
    private readonly DateTimePicker _fromPicker = new();
    private readonly DateTimePicker _toPicker = new();
    private string _mode = "revenue";

    public ReportsView()
    {
        InitializeComponent();
        SetTitle("Báo cáo vận hành", "Chọn một nhóm báo cáo; dữ liệu nặng chỉ refresh snapshot khi bấm cập nhật.");
        ConfigureToolbar();
    }

    private void ConfigureToolbar()
    {
        ConfigureModeButton(_revenueButton, "📅 Doanh thu ngày", "revenue");
        ConfigureModeButton(_usageButton, "🖥️ Hiệu suất máy", "usage");
        ConfigureModeButton(_customersButton, "👑 Top hội viên", "customers");

        _fromPicker.Format = DateTimePickerFormat.Short;
        _fromPicker.Width = 92;
        _fromPicker.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _toPicker.Format = DateTimePickerFormat.Short;
        _toPicker.Width = 92;
        _toPicker.Value = DateTime.Today;
        _fromPicker.ValueChanged += async (_, _) => await RefreshAsync();
        _toPicker.ValueChanged += async (_, _) => await RefreshAsync();

        ConfigureToolbarButton(_refreshSnapshotsButton, "⚡ Cập nhật số liệu", CyberTheme.Warning, 124);
        ConfigureToolbarButton(_exportPdfButton, "📄 PDF", CyberTheme.Primary, 72);
        ConfigureToolbarButton(_exportExcelButton, "📊 Excel", CyberTheme.Success, 78);
        _refreshSnapshotsButton.Click += async (_, _) => await RefreshSnapshotsAsync();
        _exportPdfButton.Click += async (_, _) => await ExportPdfAsync();
        _exportExcelButton.Click += async (_, _) => await ExportExcelAsync();

        Toolbar.Controls.Add(_exportExcelButton);
        Toolbar.Controls.Add(_exportPdfButton);
        Toolbar.Controls.Add(_refreshSnapshotsButton);
        Toolbar.Controls.Add(_toPicker);
        Toolbar.Controls.Add(_fromPicker);
        Toolbar.Controls.Add(_customersButton);
        Toolbar.Controls.Add(_usageButton);
        Toolbar.Controls.Add(_revenueButton);
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            switch (_mode)
            {
                case "usage":
                {
                    var rows = await RequiredContext.Repository.GetComputerUsageAsync(cancellationToken);
                    SetRows(rows, $"Hiệu suất {rows.Count} máy | {_fromPicker.Value:dd/MM/yyyy} → {_toPicker.Value:dd/MM/yyyy}");
                    break;
                }
                case "customers":
                {
                    var rows = await RequiredContext.Repository.GetTopCustomersAsync(cancellationToken);
                    SetRows(rows, $"Top {rows.Count} hội viên theo chi tiêu | snapshot hiện tại");
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
        ConfigureToolbarButton(button, text, CyberTheme.SurfaceRaised, text.Length > 18 ? 132 : 118);
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
        button.Height = 30;
        button.Margin = new Padding(3, 0, 3, 0);
        CyberTheme.StyleButton(button, color);
    }

    private async Task RefreshSnapshotsAsync()
    {
        try
        {
            await RequiredContext.Repository.RefreshMaterializedViewsAsync();
            await RefreshAsync();
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
                lines.AddRange(rows.Select(row => $"{row.ComputerCode}: {row.TotalHoursPlayed:N2} giờ | {row.TotalPlayRevenue:N0} đ"));
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
                    .Select(row => $"{row.RevenueDate:dd/MM/yyyy}: {row.NetRevenue:N0} đ | {row.TotalInvoices} hóa đơn"));
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

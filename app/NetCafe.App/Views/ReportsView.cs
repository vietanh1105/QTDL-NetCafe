// Trách nhiệm: báo cáo doanh thu, hiệu suất máy và top customer từ reporting views.

namespace NetCafe.App.Views;

public partial class ReportsView : AdminDataViewBase
{
    public ReportsView()
    {
        InitializeComponent();
        SetTitle("Báo cáo", "Chọn report trong database; view ẩn không tự refresh.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var revenue = await RequiredContext.Repository.GetDailyRevenueAsync(cancellationToken);
        var usage = await RequiredContext.Repository.GetComputerUsageAsync(cancellationToken);
        var customers = await RequiredContext.Repository.GetTopCustomersAsync(cancellationToken);
        var rows = new List<ReportRow>();
        rows.AddRange(revenue.Select(item => new ReportRow(
            "Doanh thu ngày",
            item.RevenueDate.ToString("yyyy-MM-dd"),
            $"{item.NetRevenue:N0} VND",
            $"{item.TotalInvoices} invoice")));
        rows.AddRange(usage.Select(item => new ReportRow(
            "Hiệu suất máy",
            item.ComputerCode,
            $"{item.TotalHoursPlayed:N2} giờ",
            $"{item.TotalSessions} phiên")));
        rows.AddRange(customers.Select(item => new ReportRow(
            "Top khách hàng",
            item.FullName,
            $"{item.TotalSpent:N0} VND",
            $"{item.TotalSessions} phiên")));
        SetRows(rows, $"Reporting views: {revenue.Count} ngày, {usage.Count} máy, {customers.Count} khách.");
    }

    private sealed record ReportRow(string Report, string Subject, string Value, string Detail);
}

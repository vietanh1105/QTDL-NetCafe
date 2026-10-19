// Trách nhiệm: báo cáo doanh thu, hiệu suất máy và top customer từ reporting views.

namespace NetCafe.App.Views;

public partial class ReportsView : AdminDataViewBase
{
    public ReportsView()
    {
        InitializeComponent();
        SetTitle("Báo cáo & Thống kê", "Dữ liệu tổng hợp từ các view báo cáo trong CSDL.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var revenue = await RequiredContext.Repository.GetDailyRevenueAsync(cancellationToken);
        var usage = await RequiredContext.Repository.GetComputerUsageAsync(cancellationToken);
        var customers = await RequiredContext.Repository.GetTopCustomersAsync(cancellationToken);
        var rows = new List<ReportRow>();
        rows.AddRange(revenue.Select(item => new ReportRow(
            "Doanh thu theo ngày",
            item.RevenueDate.ToString("dd/MM/yyyy"),
            $"{item.NetRevenue:N0} đ",
            $"{item.TotalInvoices} hóa đơn")));
        rows.AddRange(usage.Select(item => new ReportRow(
            "Hiệu suất máy trạm",
            item.ComputerCode,
            $"{item.TotalHoursPlayed:N2} giờ",
            $"{item.TotalSessions} phiên chơi")));
        rows.AddRange(customers.Select(item => new ReportRow(
            "Khách hàng tiêu biểu",
            item.FullName,
            $"{item.TotalSpent:N0} đ",
            $"{item.TotalSessions} phiên chơi")));
        SetRows(rows, $"Báo cáo: {revenue.Count} ngày doanh thu, {usage.Count} máy trạm, {customers.Count} khách hàng tiêu biểu.");
    }

    private sealed record ReportRow(string Report, string Subject, string Value, string Detail);
}

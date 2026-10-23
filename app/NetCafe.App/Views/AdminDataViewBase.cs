// Trách nhiệm: khung UserControl dùng chung cho bảng Admin, giảm trùng layout.
// Designer của từng view chỉ giữ metadata; dữ liệu vẫn do DbRepository cung cấp.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public abstract class AdminDataViewBase : AdminViewBase
{
    protected readonly Panel Toolbar = new();
    protected readonly Label TitleLabel = new();
    protected readonly FlowLayoutPanel ActionsPanel = new();
    protected readonly Label InfoLabel = new();
    protected readonly Button RefreshButton = new();
    protected readonly DataGridView Grid = new();

    protected AdminDataViewBase()
    {
        BackColor = BackgroundColor;
        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        Toolbar.Dock = DockStyle.Top;
        Toolbar.Height = 92;
        Toolbar.BackColor = CyberTheme.Surface;
        Toolbar.Padding = new Padding(12, 8, 12, 8);

        TitleLabel.Dock = DockStyle.Left;
        TitleLabel.AutoSize = false;
        TitleLabel.Width = 255;
        TitleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        TitleLabel.ForeColor = CyberTheme.Text;
        TitleLabel.Padding = new Padding(4, 0, 12, 0);
        TitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        InfoLabel.Dock = DockStyle.Bottom;
        InfoLabel.Height = 22;
        InfoLabel.ForeColor = CyberTheme.MutedText;
        InfoLabel.Font = new Font("Segoe UI", 8.5F);

        RefreshButton.Dock = DockStyle.Right;
        RefreshButton.Width = 52;
        RefreshButton.Height = 30;
        RefreshButton.Text = "↻";
        RefreshButton.AccessibleName = "Làm mới dữ liệu";
        CyberTheme.StyleButton(RefreshButton, PrimaryColor);
        RefreshButton.Click += async (_, _) => await RefreshAsync();

        StyleGrid(Grid);
        Grid.CellFormatting += HandleGridCellFormatting;
        Controls.Add(Grid);
        Controls.Add(InfoLabel);
        Controls.Add(Toolbar);
        ActionsPanel.Dock = DockStyle.Fill;
        ActionsPanel.FlowDirection = FlowDirection.LeftToRight;
        ActionsPanel.WrapContents = true;
        ActionsPanel.AutoScroll = true;
        ActionsPanel.BackColor = Color.Transparent;
        ActionsPanel.Padding = new Padding(12, 6, 12, 6);

        Toolbar.Controls.Add(ActionsPanel);
        Toolbar.Controls.Add(RefreshButton);
        Toolbar.Controls.Add(TitleLabel);
    }

    protected void SetTitle(string title, string info = "")
    {
        TitleLabel.Text = title;
        InfoLabel.Text = info;
    }

    protected void SetRows<T>(IEnumerable<T> rows, string info)
    {
        Grid.DataSource = rows.ToList();
        LocalizeGridHeaders(Grid);
        InfoLabel.Text = info;
    }

    private static readonly Dictionary<string, string> ColumnHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["InvoiceId"] = "Mã hóa đơn",
        ["SessionId"] = "Mã phiên",
        ["CustomerName"] = "Khách hàng",
        ["PlayAmount"] = "Tiền giờ",
        ["ServiceAmount"] = "Tiền dịch vụ",
        ["DiscountAmount"] = "Giảm giá",
        ["TotalAmount"] = "Tổng tiền",
        ["Status"] = "Trạng thái",
        ["IssuedAt"] = "Thời gian lập",
        ["Report"] = "Loại báo cáo",
        ["Subject"] = "Đối tượng",
        ["Value"] = "Giá trị",
        ["Detail"] = "Chi tiết",
        ["ServiceRequestId"] = "Mã yêu cầu",
        ["OrderId"] = "Mã đơn",
        ["ItemsSummary"] = "Món ăn & Số lượng",
        ["OrderedAt"] = "Thời gian gọi",
        ["ComputerCode"] = "Mã máy",
        ["RequestType"] = "Loại yêu cầu",
        ["Message"] = "Nội dung",
        ["CreatedAt"] = "Thời gian gửi",
        ["Module"] = "Mô-đun",
        ["Key"] = "Mã tham chiếu",
        ["Summary"] = "Nội dung tóm tắt",
        ["AlertId"] = "Mã cảnh báo",
        ["CandidateId"] = "Mã trùng lặp",
        ["ComputerId"] = "Mã máy",
        ["ComputerName"] = "Tên máy",
        ["RoomName"] = "Phòng",
        ["TypeName"] = "Loại máy",
        ["HourlyRate"] = "Giá giờ",
        ["ComputerStatus"] = "Trạng thái máy",
        ["IpAddress"] = "Địa chỉ IP",
        ["Balance"] = "Số dư ví",
        ["LoyaltyPoints"] = "Điểm tích lũy",
        ["MembershipTier"] = "Hạng hội viên",
        ["IsActive"] = "Kích hoạt",
        ["Phone"] = "Số điện thoại",
        ["Email"] = "Email",
        ["Username"] = "Tên đăng nhập",
        ["FullName"] = "Họ và tên",
        ["EmployeeId"] = "Mã nhân viên",
        ["EmpRole"] = "Vai trò",
        ["DefaultShift"] = "Ca làm việc",
        ["DbUsername"] = "Tài khoản CSDL",
        ["RevenueDate"] = "Ngày doanh thu",
        ["TotalInvoices"] = "Tổng hóa đơn",
        ["TotalPlayAmount"] = "Tổng tiền giờ",
        ["TotalServiceAmount"] = "Doanh thu dịch vụ",
        ["TotalDiscountAmount"] = "Tổng giảm giá",
        ["NetRevenue"] = "Doanh thu thuần",
        ["TotalSessions"] = "Tổng số phiên",
        ["TotalHoursPlayed"] = "Tổng giờ chơi",
        ["TotalPlayRevenue"] = "Doanh thu máy mang lại",
        ["OccupancyRate"] = "Tỷ lệ lấp đầy (%)",
        ["TotalSpent"] = "Tổng chi tiêu",
        ["LastPlayedAt"] = "Lần chơi gần nhất"
    };

    private static void LocalizeGridHeaders(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (ColumnHeaders.TryGetValue(column.Name, out var viHeader) ||
                ColumnHeaders.TryGetValue(column.DataPropertyName, out viHeader))
            {
                column.HeaderText = viHeader;
            }
        }
    }

    private static void HandleGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.Value is null || sender is not DataGridView grid)
        {
            return;
        }

        var colName = grid.Columns[e.ColumnIndex].DataPropertyName;

        if (colName.Equals("TotalHoursPlayed", StringComparison.OrdinalIgnoreCase) && e.Value is decimal hours)
        {
            e.Value = $"{hours:N1} giờ";
            e.FormattingApplied = true;
            return;
        }

        if (colName.Equals("OccupancyRate", StringComparison.OrdinalIgnoreCase) && e.Value is decimal rate)
        {
            e.Value = $"{rate:N1} %";
            e.FormattingApplied = true;
            return;
        }

        if (e.Value is decimal amount && (colName.EndsWith("Amount", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Rate", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Balance", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Spent", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Revenue", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Salary", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Cost", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Price", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Total", StringComparison.OrdinalIgnoreCase)))
        {
            e.Value = $"{amount:N0} đ";
            e.FormattingApplied = true;
            return;
        }

        if (e.Value is DateTime dt)
        {
            e.Value = dt.ToString("dd/MM/yyyy HH:mm");
            e.FormattingApplied = true;
            return;
        }

        if (e.Value is DateTimeOffset dto)
        {
            e.Value = dto.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            e.FormattingApplied = true;
            return;
        }

        if (e.Value is string text)
        {
            var localized = text.ToLowerInvariant() switch
            {
                "paid" => "Đã thanh toán",
                "unpaid" => "Chưa thanh toán",
                "refunded" => "Đã hoàn tiền",
                "cancelled" => "Đã hủy",
                "placed" => "Chờ chế biến",
                "served" => "Đã phục vụ",
                "preparing" => "Đang chuẩn bị",
                "available" => "Sẵn sàng",
                "in_use" => "Đang sử dụng",
                "maintenance" => "Bảo trì",
                "broken" => "Hỏng",
                "new" => "Mới tiếp nhận",
                "acknowledged" => "Đang xử lý",
                "resolved" => "Đã giải quyết",
                "pending" => "Chờ duyệt",
                "confirmed" => "Đã xác nhận",
                "active" => "Hoạt động",
                "finished" => "Đã kết thúc",
                "readonly" => "Chỉ đọc",
                "reviewed" => "Đã xem",
                "merged" => "Đã gộp",
                "ignored" => "Đã bỏ qua",
                "regular" => "Thành viên",
                "standard" => "Chuẩn",
                "vip" => "VIP",
                "diamond" => "Kim cương",
                "general" => "Hỗ trợ chung",
                "hardware" => "Phần cứng",
                "software" => "Phần mềm",
                "billing" => "Nạp tiền/Thanh toán",
                _ => null
            };

            if (localized is not null)
            {
                e.Value = localized;
                e.FormattingApplied = true;
            }
        }
    }
}

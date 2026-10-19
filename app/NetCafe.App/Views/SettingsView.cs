// Trách nhiệm: hiển thị connection/role/mode hiện tại; không lưu password trong UI.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class SettingsView : AdminViewBase
{
    private readonly Label _details = new();

    public SettingsView()
    {
        InitializeComponent();
        BackColor = BackgroundColor;
        _details.Dock = DockStyle.Fill;
        _details.Padding = new Padding(20);
        _details.Font = new Font("Segoe UI", 11F);
        _details.ForeColor = Color.FromArgb(30, 41, 59);
        Controls.Add(_details);
    }

    public override Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var context = RequiredContext;
        _details.Text = $"Vai trò hiện tại: {context.CurrentUser}\n" +
                        $"Mã nhân viên: {context.CurrentEmployeeId}\n" +
                        $"Thông tin kết nối: {context.ConnectionSummary}\n" +
                        $"Quyền quản lý phiên / hội viên: {(context.CanManageSessions ? "Có" : "Không")} / {(context.CanManageCustomers ? "Có" : "Không")}\n" +
                        "Cập nhật hệ thống: Bản cập nhật 08_station_customer hoạt động đầy đủ.";
        return Task.CompletedTask;
    }
}

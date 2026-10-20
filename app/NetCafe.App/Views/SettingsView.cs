// Trách nhiệm: dashboard chẩn đoán hệ thống, quyền hiện tại và listener; không hiển thị password.
// Liên kết: AdminViewContext, DatabaseRuntimeInfo, listener PostgreSQL và VACUUM ANALYZE.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class SettingsView : AdminCardViewBase
{
    private readonly Button _vacuumButton = new();

    public SettingsView()
    {
        InitializeComponent();
        _vacuumButton.Text = "🧹 VACUUM ANALYZE";
        _vacuumButton.Width = 170;
        _vacuumButton.Height = 34;
        _vacuumButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_vacuumButton, CyberTheme.Warning);
        _vacuumButton.Click += async (_, _) => await RunVacuumAsync();
        Toolbar.Controls.Add(_vacuumButton);
    }

    public override Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var context = RequiredContext;
        var cards = new[]
        {
            new AdminCardModel(
                "CSDL hiện tại",
                "Kết nối đang sử dụng",
                $"{context.ConnectionSummary}\nKhông hiển thị password.",
                "Đang kết nối",
                CyberTheme.Primary),
            new AdminCardModel(
                "Tài khoản và quyền",
                $"User: {context.CurrentUser} · Employee ID: {context.CurrentEmployeeId}",
                $"Session: {(context.CanManageSessions ? "được phép" : "bị khóa")}\nCustomer: {(context.CanManageCustomers ? "được phép" : "bị khóa")}",
                "Quyền hiện tại",
                CyberTheme.Success),
            new AdminCardModel(
                "Contract migration",
                "Station/Admin PostgreSQL contract",
                context.RuntimeInfo?.ContractStatus ?? "Chưa đọc metadata",
                context.RuntimeInfo?.IsStationContractReady == true ? "Sẵn sàng" : "Thiếu migration",
                context.RuntimeInfo?.IsStationContractReady == true ? CyberTheme.Success : CyberTheme.Danger),
            new AdminCardModel(
                "LISTEN / NOTIFY",
                $"Trạng thái: {context.ListenerStatus}",
                "Kênh: ai_security_alert, computer_state_changed, support_queue_changed, food_order_changed",
                "Listener hệ thống",
                CyberTheme.Warning),
            new AdminCardModel(
                "Dọn dẹp và bảo mật",
                "VACUUM ANALYZE theo yêu cầu",
                "Autovacuum do PostgreSQL quản lý. Bấm nút trên thanh công cụ để chạy thủ công; không reset dữ liệu.",
                "Sẵn sàng",
                CyberTheme.SurfaceRaised)
        };
        SetCards(cards, card => card, $"{cards.Length} nhóm thông tin hệ thống");
        return Task.CompletedTask;
    }

    private async Task RunVacuumAsync()
    {
        try
        {
            await RequiredContext.Repository.RunVacuumAnalyzeAsync();
            MessageBox.Show(FindForm(), "Đã chạy VACUUM ANALYZE cho các bảng vận hành.", "Dọn dẹp thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}
